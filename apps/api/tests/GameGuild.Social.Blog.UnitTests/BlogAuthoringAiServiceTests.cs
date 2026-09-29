using System.Text.Json;
using FluentAssertions;
using GameGuild.AI;
using GameGuild.Finance.Economy.Integrations.AI;
using GameGuild.Resources;
using GameGuild.Social.Blog.Authoring;
using GameGuild.Social.Blog.Configuration;
using GameGuild.Social.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Social.Blog.UnitTests;

public sealed class BlogAuthoringAiServiceTests
{
    [Fact]
    public async Task GetEntitlement_PassesThroughWalletBalanceForActor()
    {
        await using var fixture = CreateFixture();

        var entitlement = await fixture.Service.GetEntitlement(
            fixture.TenantId,
            fixture.ActorId,
            CancellationToken.None);

        entitlement.AvailableSoftCredits.Should().Be(1_000);
        entitlement.Currency.Should().Be("SoftCoin");
        await FluentActions.Invoking(() => fixture.Service.GetEntitlement(
                Guid.Empty,
                fixture.ActorId,
                CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateRun_ReservesCreditsAndQuotaAndQueuesPersistedRun()
    {
        await using var fixture = CreateFixture();
        var request = Request(fixture.PostRevision, "actor-bound-run");

        var run = await fixture.Service.CreateRun(
            fixture.TenantId,
            fixture.ActorId,
            fixture.PostId,
            request,
            CancellationToken.None);

        run.Status.Should().Be(BlogAiRunStatus.Reserved);
        run.Provider.Should().Be("OpenAi");
        run.Model.Should().Be("gpt-test");
        fixture.Credits.ReservedActors.Should().ContainSingle()
            .Which.Should().Be((fixture.TenantId, fixture.ActorId));
        fixture.Queue.Items.Should().Equal(run.Id);
        (await fixture.Db.Set<BlogAiMessage>().SingleAsync()).Role.Should().Be("user");
        (await fixture.Db.Set<BlogAiStreamEvent>().SingleAsync()).Sequence.Should().Be(1);
        fixture.Quota.Verify(service => service.TryAtomicConsumeAsync(
            fixture.TenantId,
            ResourceUsageType.AiRequests,
            1,
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.Quota.Verify(service => service.TryAtomicConsumeAsync(
            fixture.TenantId,
            ResourceUsageType.AiTokens,
            It.Is<long>(amount => amount > 64),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateRun_IdempotentReplayReturnsSameRunWithoutAnotherReservation()
    {
        await using var fixture = CreateFixture();
        var request = Request(fixture.PostRevision, "same-key");

        var first = await fixture.Service.CreateRun(
            fixture.TenantId, fixture.ActorId, fixture.PostId, request, CancellationToken.None);
        var replay = await fixture.Service.CreateRun(
            fixture.TenantId, fixture.ActorId, fixture.PostId, request, CancellationToken.None);

        replay.Id.Should().Be(first.Id);
        fixture.Credits.ReserveCalls.Should().Be(1);
        fixture.Queue.Items.Should().ContainSingle();
        (await fixture.Db.Set<BlogAiRun>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateRun_IdempotencyKeyBoundToDifferentPayload_ThrowsConflictWithoutAnotherCharge()
    {
        await using var fixture = CreateFixture();
        var request = Request(fixture.PostRevision, "bound-key");
        _ = await fixture.Service.CreateRun(
            fixture.TenantId, fixture.ActorId, fixture.PostId, request, CancellationToken.None);

        var act = () => fixture.Service.CreateRun(
            fixture.TenantId,
            fixture.ActorId,
            fixture.PostId,
            request with { Instruction = "Generate a different post" },
            CancellationToken.None);

        await act.Should().ThrowAsync<BlogAiIdempotencyConflictException>();
        fixture.Credits.ReserveCalls.Should().Be(1);
        fixture.Queue.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateRun_WhenRecoveredInRunningState_FailsClosedWithoutCallingProviderAgain()
    {
        await using var fixture = CreateFixture();
        var created = await fixture.Service.CreateRun(
            fixture.TenantId, fixture.ActorId, fixture.PostId, Request(fixture.PostRevision, "interrupted-run"), CancellationToken.None);
        var persisted = await fixture.Db.Set<BlogAiRun>().SingleAsync(item => item.Id == created.Id);
        persisted.Start(DateTimeOffset.UtcNow);
        await fixture.Db.SaveChangesAsync();

        await fixture.Service.ProcessRun(created.Id, CancellationToken.None);

        var recovered = await fixture.Service.GetRun(
            fixture.TenantId, fixture.ActorId, fixture.PostId, created.Id, CancellationToken.None);
        recovered.Status.Should().Be(BlogAiRunStatus.Failed);
        recovered.ErrorCode.Should().Be("AI_RUN_INTERRUPTED");
        fixture.Credits.ReleaseCalls.Should().Be(1);
        fixture.Ai.StreamCalls.Should().Be(0);
    }

    [Fact]
    public async Task CancelRun_BeforeProviderStarts_ReleasesCreditsAndReservedQuota()
    {
        await using var fixture = CreateFixture();
        var created = await fixture.Service.CreateRun(
            fixture.TenantId, fixture.ActorId, fixture.PostId, Request(fixture.PostRevision, "cancel-before-provider"), CancellationToken.None);

        var cancelled = await fixture.Service.CancelRun(
            fixture.TenantId, fixture.ActorId, fixture.PostId, created.Id, CancellationToken.None);
        await fixture.Service.ProcessRun(created.Id, CancellationToken.None);

        cancelled.Status.Should().Be(BlogAiRunStatus.Cancelled);
        cancelled.Usage.ReleasedAmount.Should().Be(100);
        fixture.Credits.ReleaseCalls.Should().Be(1);
        fixture.Ai.StreamCalls.Should().Be(0);
        fixture.Quota.Verify(service => service.DecrementUsageAsync(
            fixture.TenantId,
            ResourceUsageType.AiTokens,
            It.Is<long>(amount => amount > 64),
            fixture.ActorId,
            "blog-authoring",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(BlogContentFormat.Markdown, BlogAiProposalKind.LexicalPatch)]
    [InlineData(BlogContentFormat.Lexical, BlogAiProposalKind.ReplaceDocument)]
    [InlineData(BlogContentFormat.Lexical, BlogAiProposalKind.InsertAtCursor)]
    public async Task CreateRun_RejectsProposalKindsThatCannotSafelyModifyFormat(
        BlogContentFormat format,
        BlogAiProposalKind kind)
    {
        await using var fixture = CreateFixture(format: format);

        var act = () => fixture.Service.CreateRun(
            fixture.TenantId,
            fixture.ActorId,
            fixture.PostId,
            Request(fixture.PostRevision, $"rejected-{kind}") with { ProposalKind = kind },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<BlogAiProposalKindNotAllowedException>();
        exception.Which.Kind.Should().Be(kind);
        fixture.Credits.ReserveCalls.Should().Be(0);
        fixture.Queue.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessRun_PersistsDeltasProposalUsageAndConversationHistory()
    {
        await using var fixture = CreateFixture();
        var created = await fixture.Service.CreateRun(
            fixture.TenantId, fixture.ActorId, fixture.PostId, Request(fixture.PostRevision, "streamed-run"), CancellationToken.None);

        await fixture.Service.ProcessRun(created.Id, CancellationToken.None);

        var completed = await fixture.Service.GetRun(
            fixture.TenantId, fixture.ActorId, fixture.PostId, created.Id, CancellationToken.None);
        completed.Status.Should().Be(BlogAiRunStatus.Completed);
        completed.Usage.InputTokens.Should().Be(12);
        completed.Usage.OutputTokens.Should().Be(5);
        completed.Usage.SettledCost.Should().Be(17);
        completed.Proposal.Should().NotBeNull();
        completed.Proposal!.ProposedContent.Should().Be("# Improved post");

        var events = await fixture.Db.Set<BlogAiStreamEvent>()
            .OrderBy(item => item.Sequence)
            .ToListAsync();
        events.Select(item => item.Type).Should().Equal("status", "status", "delta", "delta", "completed");

        var conversations = await fixture.Service.GetConversations(
            fixture.TenantId, fixture.ActorId, fixture.PostId, CancellationToken.None);
        conversations.Should().ContainSingle();
        conversations[0].Messages.Select(message => message.Role).Should().Equal("user", "assistant");
    }

    [Fact]
    public async Task StreamRun_ReplaysPersistedEventsInSequenceOrderWithTerminalPayload()
    {
        await using var fixture = CreateFixture();
        var created = await fixture.Service.CreateRun(
            fixture.TenantId, fixture.ActorId, fixture.PostId, Request(fixture.PostRevision, "replay-stream"), CancellationToken.None);
        await fixture.Service.ProcessRun(created.Id, CancellationToken.None);

        var events = await Collect(fixture.Service.StreamRun(
            fixture.TenantId,
            fixture.ActorId,
            fixture.PostId,
            created.Id,
            -1,
            CancellationToken.None));

        events.Select(item => item.Sequence).Should().BeInAscendingOrder();
        events.Select(item => item.Type).Should().Equal("status", "status", "delta", "delta", "completed");
        events[^1].Proposal.Should().NotBeNull();
        events[^1].Usage!.SettledCost.Should().Be(17);
    }

    [Fact]
    public async Task ApplyProposal_ReplaceDocumentBumpsRevisionAndSetsContent()
    {
        await using var fixture = CreateFixture(providerOutput: "# Replacement");
        var completed = await CompleteRun(fixture, BlogAiProposalKind.ReplaceDocument, "replace-document");

        var post = await fixture.Service.ApplyProposal(
            fixture.TenantId,
            fixture.ActorId,
            fixture.PostId,
            completed.Proposal!.Id,
            new ApplyBlogAiProposalRequest(fixture.PostRevision, null),
            CancellationToken.None);

        post.Revision.Should().Be(fixture.PostRevision + 1);
        post.Content.Should().Be("# Replacement");
        completed.Proposal.Status.Should().Be(BlogAiProposalStatus.Pending);
        (await fixture.Db.Set<BlogAiProposal>().SingleAsync()).Status.Should().Be(BlogAiProposalStatus.Applied);
    }

    [Theory]
    [InlineData(null, "abcdX")]
    [InlineData(-10, "Xabcd")]
    [InlineData(2, "abXcd")]
    [InlineData(99, "abcdX")]
    public async Task ApplyProposal_InsertAtCursorClampsOffset(int? cursorOffset, string expected)
    {
        await using var fixture = CreateFixture(content: "abcd", providerOutput: "X");
        var completed = await CompleteRun(fixture, BlogAiProposalKind.InsertAtCursor, $"insert-{cursorOffset}");

        var post = await fixture.Service.ApplyProposal(
            fixture.TenantId,
            fixture.ActorId,
            fixture.PostId,
            completed.Proposal!.Id,
            new ApplyBlogAiProposalRequest(fixture.PostRevision, cursorOffset),
            CancellationToken.None);

        post.Revision.Should().Be(fixture.PostRevision + 1);
        post.Content.Should().Be(expected);
    }

    [Fact]
    public async Task ApplyProposal_LexicalPatchAppliesJsonPointerPatchToJsonBody()
    {
        var patch = JsonSerializer.Serialize(new
        {
            operations = new[] { new { op = "replace", path = "/root/children/0/children/0/text", value = "New" } },
        });
        await using var fixture = CreateFixture(format: BlogContentFormat.Lexical, jsonBody: LexicalBody(), providerOutput: patch);
        var completed = await CompleteRun(fixture, BlogAiProposalKind.LexicalPatch, "lexical-apply");

        var post = await fixture.Service.ApplyProposal(
            fixture.TenantId,
            fixture.ActorId,
            fixture.PostId,
            completed.Proposal!.Id,
            new ApplyBlogAiProposalRequest(fixture.PostRevision, null),
            CancellationToken.None);

        post.Revision.Should().Be(fixture.PostRevision + 1);
        post.JsonBody.Should().NotBeNull();
        post.JsonBody.Should().Contain("New");
        post.JsonBody.Should().Contain("\"type\": \"paragraph\"");
    }

    [Fact]
    public async Task ApplyProposal_MetadataPatchChangesOnlySeoMetadata()
    {
        var malicious = JsonSerializer.Serialize(new
        {
            title = "Improved title",
            excerpt = "Improved excerpt",
            tags = new[] { "ai", "blog" },
            metaTitle = "SEO title",
            metaDescription = "SEO description",
            ogImageUrl = "https://cdn.example.test/og.png",
            twitterCard = "summary",
            // ignored extras that must never leak into the post:
            content = "malicious body",
            jsonBody = "{\"root\":{}}",
            format = "Lexical",
            status = "Published",
        });
        await using var fixture = CreateFixture(providerOutput: malicious);
        var completed = await CompleteRun(fixture, BlogAiProposalKind.MetadataPatch, "metadata-apply");

        var post = await fixture.Service.ApplyProposal(
            fixture.TenantId,
            fixture.ActorId,
            fixture.PostId,
            completed.Proposal!.Id,
            new ApplyBlogAiProposalRequest(fixture.PostRevision, null),
            CancellationToken.None);

        post.Revision.Should().Be(fixture.PostRevision + 1);
        post.Title.Should().Be("Improved title");
        post.Excerpt.Should().Be("Improved excerpt");
        post.Tags.Should().Equal("ai", "blog");
        post.MetaTitle.Should().Be("SEO title");
        post.MetaDescription.Should().Be("SEO description");
        post.OgImageUrl.Should().Be("https://cdn.example.test/og.png");
        post.TwitterCard.Should().Be("summary");
        post.Content.Should().Be("# Original post");
        post.Format.Should().Be(BlogContentFormat.Markdown);
        post.Status.Should().Be(BlogPostStatus.Draft);
    }

    [Fact]
    public async Task ApplyProposal_WithStalePostRevision_ThrowsRevisionConflict()
    {
        await using var fixture = CreateFixture();
        var completed = await CompleteRun(fixture, BlogAiProposalKind.ReplaceDocument, "stale-apply");
        var post = await fixture.Db.Set<BlogPost>().SingleAsync();
        post.ApplyDraftEdit(fixture.PostRevision, title: "Manual edit", content: null, jsonBody: null, excerpt: null, tags: null,
            metaTitle: null, metaDescription: null, ogImageUrl: null, canonicalUrlOverride: null, twitterCard: null,
            structuredDataOverride: null, allowComments: null, readTimeMinutes: null);
        await fixture.Db.SaveChangesAsync();

        var act = () => fixture.Service.ApplyProposal(
            fixture.TenantId,
            fixture.ActorId,
            fixture.PostId,
            completed.Proposal!.Id,
            new ApplyBlogAiProposalRequest(fixture.PostRevision, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<BlogRevisionConflictException>();
    }

    [Fact]
    public async Task DiscardProposal_MarksPendingProposalTerminalWithoutChangingPost()
    {
        await using var fixture = CreateFixture();
        var completed = await CompleteRun(fixture, BlogAiProposalKind.ReplaceDocument, "discard-proposal");

        var discarded = await fixture.Service.DiscardProposal(
            fixture.TenantId,
            fixture.ActorId,
            fixture.PostId,
            completed.Proposal!.Id,
            CancellationToken.None);

        discarded.Status.Should().Be(BlogAiProposalStatus.Discarded);
        (await fixture.Db.Set<BlogPost>().SingleAsync()).Revision.Should().Be(fixture.PostRevision);
        await FluentActions.Invoking(() => fixture.Service.DiscardProposal(
                fixture.TenantId,
                fixture.ActorId,
                fixture.PostId,
                completed.Proposal.Id,
                CancellationToken.None))
            .Should().ThrowAsync<BlogAiProposalStateConflictException>();
    }

    [Fact]
    public async Task GetRun_StrangerIsDenied()
    {
        await using var fixture = CreateFixture();
        var created = await fixture.Service.CreateRun(
            fixture.TenantId, fixture.ActorId, fixture.PostId, Request(fixture.PostRevision, "private-run"), CancellationToken.None);

        var act = () => fixture.Service.GetRun(
            fixture.TenantId,
            Guid.NewGuid(),
            fixture.PostId,
            created.Id,
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CreateRun_StrangerToThePostIsDeniedBeforeCharging()
    {
        await using var fixture = CreateFixture();

        var act = () => fixture.Service.CreateRun(
            fixture.TenantId,
            Guid.NewGuid(),
            fixture.PostId,
            Request(fixture.PostRevision, "stranger-run"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        fixture.Credits.ReserveCalls.Should().Be(0);
    }

    private static async Task<BlogAiRunDto> CompleteRun(
        Fixture fixture,
        BlogAiProposalKind kind,
        string idempotencyKey)
    {
        var created = await fixture.Service.CreateRun(
            fixture.TenantId,
            fixture.ActorId,
            fixture.PostId,
            Request(fixture.PostRevision, idempotencyKey) with { ProposalKind = kind },
            CancellationToken.None);
        await fixture.Service.ProcessRun(created.Id, CancellationToken.None);
        return await fixture.Service.GetRun(
            fixture.TenantId,
            fixture.ActorId,
            fixture.PostId,
            created.Id,
            CancellationToken.None);
    }

    private static async Task<List<BlogAiStreamEventDto>> Collect(IAsyncEnumerable<BlogAiStreamEventDto> stream)
    {
        var items = new List<BlogAiStreamEventDto>();
        await foreach (var item in stream)
            items.Add(item);
        return items;
    }

    private static string LexicalBody() =>
        """{"root":{"type":"root","version":1,"children":[{"type":"paragraph","version":1,"children":[{"type":"text","version":1,"text":"Old"}]}]}}""";

    private static BlogAiRunRequest Request(int revision, string idempotencyKey) => new(
        null,
        revision,
        "Improve this post",
        BlogAiProposalKind.ReplaceDocument,
        null,
        idempotencyKey);

    private static Fixture CreateFixture(
        bool failGeneration = false,
        ResourceUsageType? quotaExceeded = null,
        BlogContentFormat format = BlogContentFormat.Markdown,
        string? content = null,
        string? jsonBody = null,
        string? providerOutput = null)
    {
        var db = new BlogAiTestDbContext(
            new DbContextOptionsBuilder<BlogAiTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options);
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var post = BlogPost.Create(actorId, "Post", "post", format, tenantId);
        post.ApplyDraftEdit(
            1,
            title: null,
            content: content ?? (format == BlogContentFormat.Markdown ? "# Original post" : null),
            jsonBody: jsonBody,
            excerpt: null,
            tags: null,
            metaTitle: null,
            metaDescription: null,
            ogImageUrl: null,
            canonicalUrlOverride: null,
            twitterCard: null,
            structuredDataOverride: null,
            allowComments: null,
            readTimeMinutes: null);
        db.Set<BlogPost>().Add(post);
        db.SaveChanges();
        var postId = post.Id;

        var quota = new Mock<IResourceQuotaEnforcer>();
        quota.Setup(service => service.TryAtomicConsumeAsync(
                tenantId,
                It.IsAny<ResourceUsageType>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, ResourceUsageType type, long amount, CancellationToken _) =>
                (type != quotaExceeded, type == quotaExceeded ? 100 : amount, type == quotaExceeded ? 100L : null));
        quota.Setup(service => service.DecrementUsageAsync(
                tenantId,
                It.IsAny<ResourceUsageType>(),
                It.IsAny<long>(),
                actorId,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var credits = new RecordingCredits();
        var queue = new RecordingQueue();
        var ai = new RecordingAiOrchestrator(failGeneration, providerOutput ?? "# Improved post");
        var service = new BlogAuthoringAiService(
            db,
            ai,
            credits,
            quota.Object,
            queue,
            TimeProvider.System,
            NullLogger<BlogAuthoringAiService>.Instance);
        return new Fixture(db, service, credits, queue, quota, ai, tenantId, actorId, postId, post.Revision);
    }

    private sealed record Fixture(
        BlogAiTestDbContext Db,
        BlogAuthoringAiService Service,
        RecordingCredits Credits,
        RecordingQueue Queue,
        Mock<IResourceQuotaEnforcer> Quota,
        RecordingAiOrchestrator Ai,
        Guid TenantId,
        Guid ActorId,
        Guid PostId,
        int PostRevision) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class RecordingAiOrchestrator(bool failGeneration, string providerOutput) : IAiOrchestrator
    {
        public int DescribeCalls { get; private set; }
        public int StreamCalls { get; private set; }
        public AiGenerateRequest? LastDescribeRequest { get; private set; }
        public AiGenerateRequest? LastStreamRequest { get; private set; }

        public Task<Result<AiResolvedModelDto>> DescribeGenerateAsync(
            AiExecutionActor actor,
            AiGenerateRequest request,
            CancellationToken cancellationToken = default)
        {
            DescribeCalls++;
            LastDescribeRequest = request;
            return Task.FromResult(Result.Success(new AiResolvedModelDto("OpenAi", "gpt-test", 64)));
        }

        public async Task<Result<AiCompletionResponse>> GenerateForActorStreamingAsync(
            AiExecutionActor actor,
            AiGenerateRequest request,
            Func<string, CancellationToken, ValueTask> onDelta,
            CancellationToken cancellationToken = default)
        {
            StreamCalls++;
            LastStreamRequest = request;
            if (failGeneration)
                return Result.Failure<AiCompletionResponse>(Error.Problem("AI.ProviderFailed", "Provider failed."));
            if (providerOutput == "# Improved post")
            {
                await onDelta("# Improved ", cancellationToken);
                await onDelta("post", cancellationToken);
            }
            else
            {
                await onDelta(providerOutput, cancellationToken);
            }
            return Result.Success(new AiCompletionResponse(
                "OpenAi", "gpt-test", providerOutput, "stop", new AiUsageDto(12, 5, 17)));
        }

        public Task<Result<AiCompletionResponse>> GenerateForActorStreamingWithReservedQuotaAsync(
            AiExecutionActor actor,
            AiGenerateRequest request,
            Func<string, CancellationToken, ValueTask> onDelta,
            CancellationToken cancellationToken = default) =>
            GenerateForActorStreamingAsync(actor, request, onDelta, cancellationToken);

        public Task<Result<AiCompletionResponse>> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Result<AiCompletionResponse>> GenerateAsync(AiGenerateRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Result<AiCompletionResponse>> GenerateForActorAsync(AiExecutionActor actor, AiGenerateRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingCredits : IAiCreditWalletService
    {
        private readonly Dictionary<Guid, AiCreditReservation> _reservations = [];
        public List<(Guid TenantId, Guid ActorId)> ReservedActors { get; } = [];
        public int ReserveCalls { get; private set; }
        public int ReleaseCalls { get; private set; }

        public Task<AiCreditBalance> GetBalanceAsync(Guid tenantId, Guid actorId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiCreditBalance(1_000, 0, _reservations.Values.Sum(item => item.SettledSoftUnits)));

        public Task<AiCreditQuote> QuoteAsync(string serviceCode, string provider, string model, int maximumInputTokens, int maximumOutputTokens, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiCreditQuote("test-v1", provider, model, maximumInputTokens, maximumOutputTokens, 100, 1_000_000, 1_000_000));

        public Task<AiCreditReservation> ReserveAsync(Guid runId, Guid tenantId, Guid actorId, string serviceCode, AiCreditQuote quote, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            ReserveCalls++;
            ReservedActors.Add((tenantId, actorId));
            var reservation = AiCreditReservation.Create(
                runId, tenantId, actorId, Guid.NewGuid(), serviceCode, quote.Provider, quote.Model,
                quote.MaximumSoftUnits, idempotencyKey, DateTimeOffset.UtcNow, quote.RateCardVersion,
                quote.InputSoftUnitsPerMillion, quote.OutputSoftUnitsPerMillion);
            _reservations.Add(runId, reservation);
            return Task.FromResult(reservation);
        }

        public Task<AiCreditReservation> SettleAsync(Guid runId, int inputTokens, int outputTokens, string providerUsageId, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var reservation = _reservations[runId];
            reservation.Settle(inputTokens, outputTokens, inputTokens + outputTokens, providerUsageId, idempotencyKey, DateTimeOffset.UtcNow);
            return Task.FromResult(reservation);
        }

        public Task<AiCreditReservation> ReleaseAsync(Guid runId, string reason, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            ReleaseCalls++;
            var reservation = _reservations[runId];
            reservation.Release(reason, idempotencyKey, DateTimeOffset.UtcNow);
            return Task.FromResult(reservation);
        }
    }

    private sealed class RecordingQueue : IBlogAuthoringAiRunQueue
    {
        public List<Guid> Items { get; } = [];

        public ValueTask Enqueue(Guid runId, CancellationToken cancellationToken = default)
        {
            Items.Add(runId);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<Guid> ReadAll([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var item in Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
                await Task.Yield();
            }
        }
    }

    private sealed class BlogAiTestDbContext(DbContextOptions<BlogAiTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new BlogModelConfiguration().Configure(modelBuilder);
            modelBuilder.Entity<SocialProfile>(builder =>
            {
                builder.HasKey(profile => profile.Id);
                builder.Ignore(profile => profile.Skills);
                builder.Ignore(profile => profile.PortfolioItems);
            });
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
