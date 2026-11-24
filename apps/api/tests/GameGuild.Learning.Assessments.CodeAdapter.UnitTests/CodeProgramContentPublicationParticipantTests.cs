using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Assessments.CodeAdapter;
using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Authoring;
using GameGuild.Learning.Assessments.Grading.Capabilities;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Assessments.Grading.Persistence;
using GameGuild.Learning.Assessments.Grading.Runtime;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Grading.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Learning.Assessments.CodeAdapter.Tests;

public sealed class CodeProgramContentPublicationParticipantTests
{
    [Theory]
    [InlineData("function")]
    [InlineData("standard-io")]
    public async Task Prepare_StagesTheAssessmentWithoutSavingTrackedContent(string family)
    {
        await using var fixture = new PublicationFixture();
        await fixture.InitializeAsync();
        var version = fixture.Content.Version;
        var saveCalls = fixture.Context.SaveCalls;
        var payload = fixture.Payload(family) with { Title = "New exercise", Slug = "new-exercise", Description = "New instructions" };

        await fixture.Participant.PreparePublishAsync(fixture.Content, payload, fixture.ActorId);

        fixture.Context.SaveCalls.Should().Be(saveCalls, "preparation must leave the publication unit of work unsaved");
        fixture.Content.Version.Should().Be(version);
        var assessment = fixture.Context.ChangeTracker.Entries<Assessment>().Single();
        assessment.State.Should().Be(EntityState.Added);
        assessment.Entity.ContentId.Should().Be(fixture.Content.Id);
        assessment.Entity.CourseId.Should().Be(fixture.Content.ProgramId);
        assessment.Entity.TenantId.Should().Be(fixture.TenantId);
        assessment.Entity.Title.Should().Be(payload.Title);
        assessment.Entity.Slug.Should().Be(payload.Slug);
        assessment.Entity.Description.Should().Be(payload.Description);
        assessment.Entity.Type.Should().Be(AssessmentType.Assignment);
        assessment.Entity.MaxScore.Should().Be(ScoreValue.FromUnits(10_000));
        assessment.Entity.SubmissionModalities.Should().Be(SubmissionModality.Code);
        assessment.Entity.PresentationMode.Should().Be(AssessmentPresentationMode.SingleStep);
        assessment.Entity.ReviewMethods.Should().Be(ReviewMethods.AutomatedReview | ReviewMethods.InstructorReview);
        assessment.Entity.ReviewConfigurationCanonicalJson.Should().Be(
            "{\"instructor\":{\"requireOverrideReason\":false},\"schemaVersion\":1}");
    }

    [Theory]
    [InlineData("function")]
    [InlineData("standard-io")]
    public async Task PublishAndRepublish_KeepTheDraftBaseAndPersistedContentVersionAligned(string family)
    {
        await using var fixture = new PublicationFixture();
        await fixture.InitializeAsync();
        var first = await fixture.PublishAsync(fixture.Payload(family));

        first.Draft.BasePublishedVersion.Should().Be(first.PublishedContent.Version);
        fixture.Context.ChangeTracker.Clear();
        var content = await fixture.Context.Set<ProgramContent>().SingleAsync();
        first.Draft.BasePublishedVersion.Should().Be(content.Version);
        var assessment = await fixture.Context.Set<Assessment>().SingleAsync();
        var firstRevisionId = assessment.PublishedDefinitionRevisionId
            ?? throw new InvalidOperationException("The first publication must have an executable revision.");
        var firstRevision = await fixture.Context.Set<AssessmentDefinitionRevision>().SingleAsync(value => value.Id == firstRevisionId);
        var firstSnapshot = firstRevision.ExecutionSnapshotCanonicalJson;
        var firstHash = firstRevision.ExecutionSnapshotHash;
        var document = JsonNode.Parse(fixture.Payload(family).JsonBody!.Value.GetRawText())!.AsObject();
        document["Problem"]!["Statement"] = "Updated problem for the next revision.";

        var second = await fixture.PublishAsync(fixture.Payload(family) with { JsonBody = JsonSerializer.SerializeToElement(document) });

        second.Draft.BasePublishedVersion.Should().Be(second.PublishedContent.Version).And.BeGreaterThan(first.Draft.BasePublishedVersion);
        fixture.Context.ChangeTracker.Clear();
        var reopened = await fixture.Authoring.GetOrCreateDraft(fixture.Content.ProgramId, fixture.Content.Id, fixture.ActorId, CancellationToken.None);
        reopened.BasePublishedVersion.Should().Be((await fixture.Context.Set<ProgramContent>().SingleAsync()).Version);
        reopened.Revision.Should().Be(second.Draft.Revision);
        var revisions = await fixture.Context.Set<AssessmentDefinitionRevision>().ToListAsync();
        revisions.Should().HaveCount(2);
        var retained = revisions.Single(value => value.Id == firstRevisionId);
        string.Equals(retained.ExecutionSnapshotCanonicalJson, firstSnapshot, StringComparison.Ordinal)
            .Should().BeTrue("a later publication must not change the protected earlier snapshot");
        retained.ExecutionSnapshotHash.Should().Be(firstHash);
        (await fixture.Context.Set<Assessment>().SingleAsync()).PublishedDefinitionRevisionId.Should().NotBe(firstRevisionId);
        var audits = await fixture.Context.Set<ProgramContentPublicationAudit>().OrderBy(value => value.PublishedVersion).ToListAsync();
        audits.Select(value => value.PublishedVersion).Should().Equal(first.PublishedContent.Version, second.PublishedContent.Version);
    }

    [Fact]
    public async Task Prepare_GlobalContentUsesTheAuthenticatedTenantWithoutSaving()
    {
        await using var fixture = new PublicationFixture();
        fixture.Content.TenantId = null;
        await fixture.InitializeAsync();
        var saveCalls = fixture.Context.SaveCalls;

        await fixture.Participant.PreparePublishAsync(fixture.Content, fixture.Payload("standard-io"), fixture.ActorId);

        fixture.Context.SaveCalls.Should().Be(saveCalls);
        fixture.Context.ChangeTracker.Entries<Assessment>().Single().Entity.TenantId.Should().Be(fixture.TenantId);
        fixture.Content.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Prepare_UpdatesTheScoreWithoutSavingOrReplacingTheExistingAcademicPolicy()
    {
        await using var fixture = new PublicationFixture();
        await fixture.InitializeAsync();
        var groupId = Guid.NewGuid();
        var assessment = Assessment.Create(fixture.Content.ProgramId, "Existing", AssessmentType.Assignment,
            ScoreValue.FromUnits(20_000), assessmentGroupId: groupId, contentId: fixture.Content.Id,
            reviewMethods: ReviewMethods.AutomatedReview | ReviewMethods.InstructorReview);
        assessment.TenantId = fixture.TenantId;
        assessment.SetPassingScore(ScoreValue.FromUnits(1_000));
        assessment.SetReviewPolicy(assessment.ReviewMethods,
            "{\"schemaVersion\":1,\"instructor\":{\"requireOverrideReason\":true}}",
            AttemptContributionMode.FirstFinalized, ContentCompletionMode.OnFinalize, ResultReleaseMode.Manual, null);
        fixture.Context.Add(assessment);
        await fixture.Context.SaveChangesAsync();
        var saveCalls = fixture.Context.SaveCalls;
        var version = assessment.Version;
        var configuration = assessment.ReviewConfigurationCanonicalJson;

        await fixture.Participant.PreparePublishAsync(fixture.Content, fixture.Payload("function"), fixture.ActorId);

        fixture.Context.SaveCalls.Should().Be(saveCalls);
        assessment.Version.Should().Be(version);
        assessment.MaxScore.Should().Be(ScoreValue.FromUnits(10_000));
        assessment.PassingScore.Should().Be(ScoreValue.FromUnits(1_000));
        assessment.AssessmentGroupId.Should().Be(groupId);
        assessment.ReviewConfigurationCanonicalJson.Should().Be(configuration);
        assessment.AttemptContributionMode.Should().Be(AttemptContributionMode.FirstFinalized);
        assessment.ContentCompletionMode.Should().Be(ContentCompletionMode.OnFinalize);
        assessment.ResultReleaseMode.Should().Be(ResultReleaseMode.Manual);
        fixture.Context.ChangeTracker.Clear();
        (await fixture.Context.Set<Assessment>().SingleAsync()).MaxScore.Should().Be(ScoreValue.FromUnits(20_000));
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("unauthenticated")]
    [InlineData("missing-tenant")]
    [InlineData("content-type")]
    [InlineData("content-tenant")]
    [InlineData("assessment-tenant")]
    [InlineData("assessment-course")]
    [InlineData("assessment-type")]
    public async Task Prepare_RejectsWrongActorOrScopeBeforeSavingOrMutatingAnAssessment(string mismatch)
    {
        await using var fixture = new PublicationFixture();
        if (mismatch == "content-type")
        {
            fixture.Content.Type = ProgramContentType.Questionnaire;
        }
        await fixture.InitializeAsync();
        Assessment? assessment = null;
        if (mismatch.StartsWith("assessment-", StringComparison.Ordinal))
        {
            assessment = Assessment.Create(mismatch == "assessment-course" ? Guid.NewGuid() : fixture.Content.ProgramId,
                "Existing", mismatch == "assessment-type" ? AssessmentType.Quiz : AssessmentType.Assignment,
                ScoreValue.FromUnits(20_000), contentId: fixture.Content.Id);
            assessment.TenantId = mismatch == "assessment-tenant" ? Guid.NewGuid() : fixture.TenantId;
            fixture.Context.Add(assessment);
            await fixture.Context.SaveChangesAsync();
        }
        using var actorScope = fixture.Actors.BeginActorContextScope(fixture.Actors.ActorContext with
        {
            SubjectId = mismatch == "actor" ? Guid.NewGuid().ToString() : fixture.ActorId.ToString(),
            TenantId = mismatch == "missing-tenant" ? null : mismatch == "content-tenant" ? Guid.NewGuid() : fixture.TenantId,
            IsAuthenticated = mismatch != "unauthenticated",
        });
        var saveCalls = fixture.Context.SaveCalls;

        Func<Task> prepare = () => fixture.Participant.PreparePublishAsync(fixture.Content, fixture.Payload("standard-io"), fixture.ActorId);

        await prepare.Should().ThrowAsync<ValidationException>();
        fixture.Context.SaveCalls.Should().Be(saveCalls);
        fixture.Context.ChangeTracker.Entries<Assessment>().Should().HaveCount(assessment is null ? 0 : 1);
        if (assessment is not null)
        {
            assessment.MaxScore.Should().Be(ScoreValue.FromUnits(20_000));
            fixture.Context.Entry(assessment).State.Should().Be(EntityState.Unchanged);
        }
    }

    [Fact]
    public async Task Prepare_RejectsAnInvalidDefinitionWithoutSavingOrCreatingTheAssessment()
    {
        await using var fixture = new PublicationFixture();
        await fixture.InitializeAsync();
        var saveCalls = fixture.Context.SaveCalls;

        Func<Task> prepare = () => fixture.Participant.PreparePublishAsync(fixture.Content,
            fixture.Payload("function") with { JsonBody = null }, fixture.ActorId);

        await prepare.Should().ThrowAsync<ValidationException>();
        fixture.Context.SaveCalls.Should().Be(saveCalls);
        fixture.Context.ChangeTracker.Entries<Assessment>().Should().BeEmpty();
    }

    private sealed class PublicationFixture : IAsyncDisposable
    {
        public Guid ActorId { get; } = Guid.NewGuid();
        public Guid TenantId { get; } = Guid.NewGuid();
        public ActorContextAccessor Actors { get; } = new();
        public PublicationContext Context { get; }
        public ProgramContent Content { get; }
        public IProgramContentPublicationParticipant Participant { get; }
        public ProgramContentAuthoringService Authoring { get; }
        private readonly ServiceProvider provider;
        private readonly IActorContextScope actorScope;

        public PublicationFixture()
        {
            Context = new PublicationContext(new DbContextOptionsBuilder<PublicationContext>()
                .UseInMemoryDatabase($"CodeLessonPublication_{Guid.NewGuid()}").Options);
            Content = new ProgramContent
            {
                Id = Guid.NewGuid(),
                ProgramId = Guid.NewGuid(),
                TenantId = TenantId,
                Title = "Exercise",
                Slug = "exercise",
                Type = ProgramContentType.Code,
                Visibility = Visibility.Public,
            };
            actorScope = Actors.BeginActorContextScope(ActorContext.Anonymous with
            {
                ActorKind = ActorKind.User,
                SubjectId = ActorId.ToString(),
                TenantId = TenantId,
                IsAuthenticated = true,
            });
            var services = new ServiceCollection();
            services.AddAssessmentsModule();
            services.AddCodeGradingAdapter();
            services.AddSingleton<IApplicationDbContext>(Context);
            services.AddSingleton<IActorContextAccessor>(Actors);
            services.AddSingleton<IAssessmentAuthoringService>(registered => new AssessmentAuthoringService(Context,
                registered.GetRequiredService<IAssessmentTypeAdapterResolver>(),
                registered.GetRequiredService<IAssessmentExecutionPolicyResolver>(),
                registered.GetRequiredService<IReviewStageHandlerResolver>(),
                new AIReviewProviderRegistry([]), registered.GetServices<IReviewStageHandler>(), [],
                new AcademicOutboxWriter(Context, []), NullLogger<AssessmentAuthoringService>.Instance));
            provider = services.BuildServiceProvider();
            Participant = provider.GetRequiredService<IProgramContentPublicationParticipant>();
            Authoring = new ProgramContentAuthoringService(Context, publicationParticipants: [Participant]);
        }

        public async Task InitializeAsync()
        {
            Context.Add(Content);
            await Context.SaveChangesAsync();
        }

        public AuthoringContentPayload Payload(string family) => AuthoringContentPayload.From(Content) with
        {
            JsonBody = JsonSerializer.SerializeToElement(CodeExecutionConfigurationAdapterTests.Definition(family), CodeAssessmentContracts.ContentJson),
        };

        public async Task<PublishAuthoringResult> PublishAsync(AuthoringContentPayload payload)
        {
            var draft = await Authoring.GetOrCreateDraft(Content.ProgramId, Content.Id, ActorId, CancellationToken.None);
            var saved = await Authoring.SaveDraft(Content.ProgramId, Content.Id, draft.Revision, payload, ActorId, CancellationToken.None);
            return await Authoring.Publish(Content.ProgramId, Content.Id, saved.Revision, ActorId, CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            await provider.DisposeAsync();
            await Context.DisposeAsync();
            actorScope.Dispose();
        }
    }

    private sealed class PublicationContext(DbContextOptions<PublicationContext> options) : DbContext(options), IApplicationDbContext
    {
        public int SaveCalls { get; private set; }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Database.BeginTransactionAsync(cancellationToken);

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            // Mirror the host's entity-version increment so an intermediate save is observable.
            foreach (var entry in ChangeTracker.Entries<EntityBase<Guid>>().Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                entry.Entity.Version++;
            }
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new AssessmentsModelConfiguration().Configure(modelBuilder);
            new GradingPersistenceModelConfiguration().Configure(modelBuilder);
            modelBuilder.Entity<ProgramContent>(entity =>
            {
                entity.Ignore(value => value.Program);
                entity.Ignore(value => value.Parent);
                entity.Ignore(value => value.Children);
                entity.Ignore(value => value.ContentInteractions);
            });
            modelBuilder.Entity<ProgramContentDraft>().HasKey(value => value.Id);
            modelBuilder.Entity<ProgramContentDraft>().Ignore(value => value.ETag);
            modelBuilder.Entity<ProgramContentPublicationAudit>().HasKey(value => value.Id);
        }
    }
}
