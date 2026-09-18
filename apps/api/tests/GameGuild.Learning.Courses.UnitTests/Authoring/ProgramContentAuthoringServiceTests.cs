using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;
using GameGuild.Assets;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace GameGuild.Learning.Courses.UnitTests.Authoring;

public sealed class ProgramContentAuthoringServiceTests
{
    [Fact]
    public async Task SaveAndPublish_ValidatesAssetManifestAndPromotesPrivateAssets()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var content = PublishedContent("Asset lesson", "Original");
        content.TenantId = tenantId;
        var assetContent = new AssetContent("bucket", "key", new string('a', 64), "image/png", 4, null, null)
        {
            TenantId = tenantId,
            VirusScanStatus = VirusScanStatus.Clean,
            ModerationStatus = ModerationStatus.Approved,
        };
        var asset = new AssetReference(
            assetContent.Id,
            actorId,
            "diagram.png",
            AssetAccessPolicy.Private,
            nameof(ProgramContent),
            content.Id)
        {
            TenantId = tenantId,
            Content = assetContent,
        };
        context.AddRange(content, assetContent, asset);
        await context.SaveChangesAsync();
        var manifest = new LearningAssetManifestService(context);
        var service = new ProgramContentAuthoringService(context, manifest);
        var draft = await service.GetOrCreateDraft(content.ProgramId, content.Id, actorId, CancellationToken.None);
        var portablePayload = draft.Payload with { Body = $"![diagram](asset://{asset.Id})" };

        var saved = await service.SaveDraft(content.ProgramId, content.Id, draft.Revision, portablePayload, actorId, CancellationToken.None);
        await service.Publish(content.ProgramId, content.Id, saved.Revision, actorId, CancellationToken.None);

        asset.AccessPolicy.Should().Be(AssetAccessPolicy.Inherited);
        (await manifest.IsInUseAsync(asset.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task Publish_WhenAssetWasRemovedFromLesson_DemotesItToPrivate()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var content = PublishedContent("Asset lesson", "Original");
        content.TenantId = tenantId;
        var assetContent = new AssetContent("bucket", "key", new string('b', 64), "image/png", 4, null, null)
        {
            TenantId = tenantId,
            VirusScanStatus = VirusScanStatus.Clean,
            ModerationStatus = ModerationStatus.Approved,
        };
        var asset = new AssetReference(
            assetContent.Id,
            actorId,
            "old-diagram.png",
            AssetAccessPolicy.Inherited,
            nameof(ProgramContent),
            content.Id)
        {
            TenantId = tenantId,
            Content = assetContent,
        };
        content.Body = $"![diagram](asset://{asset.Id})";
        context.AddRange(content, assetContent, asset);
        await context.SaveChangesAsync();
        var manifest = new LearningAssetManifestService(context);
        var service = new ProgramContentAuthoringService(context, manifest);
        var draft = await service.GetOrCreateDraft(content.ProgramId, content.Id, actorId, CancellationToken.None);
        var saved = await service.SaveDraft(
            content.ProgramId,
            content.Id,
            draft.Revision,
            draft.Payload with { Body = "The image was removed." },
            actorId,
            CancellationToken.None);

        await service.Publish(content.ProgramId, content.Id, saved.Revision, actorId, CancellationToken.None);

        asset.AccessPolicy.Should().Be(AssetAccessPolicy.Private);
        (await manifest.IsInUseAsync(asset.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task SaveDraft_RejectsAnAssetMissingFromTheAuthoritativeContentScope()
    {
        await using var context = CreateContext();
        var content = PublishedContent("Asset lesson", "Original");
        content.TenantId = Guid.NewGuid();
        context.Add(content);
        await context.SaveChangesAsync();
        var service = new ProgramContentAuthoringService(context, new LearningAssetManifestService(context));
        var draft = await service.GetOrCreateDraft(content.ProgramId, content.Id, Guid.NewGuid(), CancellationToken.None);
        var missingAssetId = Guid.NewGuid();

        var save = () => service.SaveDraft(
            content.ProgramId,
            content.Id,
            draft.Revision,
            draft.Payload with { Body = $"asset://{missingAssetId}" },
            Guid.NewGuid(),
            CancellationToken.None);

        await save.Should().ThrowAsync<ValidationException>()
            .WithMessage("*not available in this lesson*");
    }

    [Fact]
    public async Task GetDraft_CreatesSharedDraftFromPublishedContent()
    {
        await using var context = CreateContext();
        var actorId = Guid.NewGuid();
        var content = PublishedContent("Published title", "Published body");
        context.Add(content);
        await context.SaveChangesAsync();
        var service = new ProgramContentAuthoringService(context);

        var draft = await service.GetOrCreateDraft(content.ProgramId, content.Id, actorId, CancellationToken.None);

        draft.Revision.Should().Be(1);
        draft.BasePublishedVersion.Should().Be(content.Version);
        draft.Payload.Title.Should().Be("Published title");
        draft.Payload.Body.Should().Be("Published body");
        (await context.Set<ProgramContentDraft>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveDraft_WithStaleRevision_DoesNotOverwriteNewerPayload()
    {
        await using var context = CreateContext();
        var content = PublishedContent("Lesson", "Original");
        context.Add(content);
        await context.SaveChangesAsync();
        var service = new ProgramContentAuthoringService(context);
        var draft = await service.GetOrCreateDraft(content.ProgramId, content.Id, Guid.NewGuid(), CancellationToken.None);
        var first = draft.Payload with { Body = "First save" };
        await service.SaveDraft(content.ProgramId, content.Id, 1, first, Guid.NewGuid(), CancellationToken.None);

        var act = () => service.SaveDraft(
            content.ProgramId,
            content.Id,
            1,
            draft.Payload with { Body = "Stale save" },
            Guid.NewGuid(),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthoringRevisionConflictException>();
        var persisted = await context.Set<ProgramContentDraft>().SingleAsync();
        persisted.PayloadJson.Should().Contain("First save");
        persisted.PayloadJson.Should().NotContain("Stale save");
    }

    [Fact]
    public async Task Publish_AppliesDraftAtomicallyAndRecordsActorAudit()
    {
        await using var context = CreateContext();
        var actorId = Guid.NewGuid();
        var content = PublishedContent("Lesson", "Published body");
        context.Add(content);
        await context.SaveChangesAsync();
        var service = new ProgramContentAuthoringService(context);
        var draft = await service.GetOrCreateDraft(content.ProgramId, content.Id, actorId, CancellationToken.None);
        await service.SaveDraft(
            content.ProgramId,
            content.Id,
            draft.Revision,
            draft.Payload with { Title = "New title", Body = "New published body" },
            actorId,
            CancellationToken.None);

        var published = await service.Publish(content.ProgramId, content.Id, 2, actorId, CancellationToken.None);

        published.PublishedContent.Title.Should().Be("New title");
        published.PublishedContent.Body.Should().Be("New published body");
        published.Draft.BasePublishedVersion.Should().Be(published.PublishedContent.Version);
        var audit = await context.Set<ProgramContentPublicationAudit>().SingleAsync();
        audit.PublishedBy.Should().Be(actorId);
        audit.ContentId.Should().Be(content.Id);
    }

    [Fact]
    public async Task GetDraft_RequiresAuthenticatedAuthor()
    {
        await using var context = CreateContext();
        var service = new ProgramContentAuthoringService(context);

        var act = () => service.GetOrCreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetDraft_RejectsContentFromAnotherProgram()
    {
        await using var context = CreateContext();
        var content = PublishedContent("Wrong course", "Body");
        context.Add(content);
        await context.SaveChangesAsync();
        var service = new ProgramContentAuthoringService(context);

        var act = () => service.GetOrCreateDraft(
            Guid.NewGuid(),
            content.Id,
            Guid.NewGuid(),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found in this course*");
    }

    [Fact]
    public async Task GetDraft_RejectsPersistedNullPayload()
    {
        await using var context = CreateContext();
        var actorId = Guid.NewGuid();
        var content = PublishedContent("Invalid draft", "Body");
        var draft = ProgramContentDraft.Create(
            Guid.NewGuid(),
            content.ProgramId,
            content.Id,
            actorId,
            1,
            "null",
            DateTimeOffset.UtcNow);
        context.AddRange(content, draft);
        await context.SaveChangesAsync();
        var service = new ProgramContentAuthoringService(context);

        var act = () => service.GetOrCreateDraft(
            content.ProgramId,
            content.Id,
            actorId,
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*draft payload is invalid*");
    }

    [Fact]
    public async Task Publish_NormalizesBlankSlugAndPersistsStructuredActivityPayload()
    {
        await using var context = CreateContext();
        var actorId = Guid.NewGuid();
        var content = PublishedContent("Activity", "Original");
        context.Add(content);
        await context.SaveChangesAsync();
        var service = new ProgramContentAuthoringService(context);
        var draft = await service.GetOrCreateDraft(
            content.ProgramId,
            content.Id,
            actorId,
            CancellationToken.None);
        var jsonBody = JsonDocument.Parse("{\"root\":{\"children\":[]}}").RootElement.Clone();
        var payload = draft.Payload with
        {
            Title = "Discussion activity",
            Slug = "   ",
            Type = ProgramContentType.Discussion,
            JsonBody = jsonBody,
            LessonFormat = null,
            ActivitySettings = new DiscussionActivitySettings(
                AllowReplies: true,
                RequireThreadRoot: false,
                MinimumBodyLength: 10,
                MaximumBodyLength: 500),
        };
        var saved = await service.SaveDraft(
            content.ProgramId,
            content.Id,
            draft.Revision,
            payload,
            actorId,
            CancellationToken.None);

        var result = await service.Publish(
            content.ProgramId,
            content.Id,
            saved.Revision,
            actorId,
            CancellationToken.None);

        result.PublishedContent.Slug.Should().Be("discussion-activity");
        content.JsonBody.Should().BeNull("activity normalization stores its typed settings instead of lesson JSON");
        var settings = content.GetActivitySettings().Should().BeOfType<DiscussionActivitySettings>().Which;
        settings.AllowReplies.Should().BeTrue();
        settings.RequireThreadRoot.Should().BeFalse();
        settings.MinimumBodyLength.Should().Be(10);
        settings.MaximumBodyLength.Should().Be(500);
    }

    private static ProgramContent PublishedContent(string title, string body) => new()
    {
        Id = Guid.NewGuid(),
        ProgramId = Guid.NewGuid(),
        Title = title,
        Slug = title.ToSlugCase(),
        Body = body,
        Type = ProgramContentType.Lesson,
        LessonFormat = LessonContentFormat.Markdown,
    };

    private static AuthoringTestDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AuthoringTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class AuthoringTestDbContext(DbContextOptions<AuthoringTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProgramContent>().HasKey(candidate => candidate.Id);
            modelBuilder.Entity<ProgramContent>().Ignore(candidate => candidate.Program);
            modelBuilder.Entity<ProgramContent>().Ignore(candidate => candidate.Parent);
            modelBuilder.Entity<ProgramContent>().Ignore(candidate => candidate.Children);
            modelBuilder.Entity<ProgramContent>().Ignore(candidate => candidate.ContentInteractions);
            modelBuilder.Entity<ProgramContent>().Ignore(candidate => candidate.FullPath);
            modelBuilder.Entity<ProgramContent>().Ignore(candidate => candidate.ChildCount);
            modelBuilder.Entity<ProgramContent>().Ignore(candidate => candidate.HasChildren);
            modelBuilder.Entity<ProgramContentDraft>().HasKey(candidate => candidate.Id);
            modelBuilder.Entity<ProgramContentDraft>().Ignore(candidate => candidate.ETag);
            modelBuilder.Entity<ProgramContentPublicationAudit>().HasKey(candidate => candidate.Id);
            modelBuilder.Entity<AssetContent>().HasKey(candidate => candidate.Id);
            modelBuilder.Entity<AssetContent>().Ignore(candidate => candidate.References);
            modelBuilder.Entity<AssetContent>().Ignore(candidate => candidate.TransformedVersions);
            modelBuilder.Entity<AssetReference>().HasKey(candidate => candidate.Id);
            modelBuilder.Entity<AssetReference>().Ignore(candidate => candidate.Reports);
            modelBuilder.Entity<AssetReference>().Ignore(candidate => candidate.Localizations);
            modelBuilder.Entity<AssetReference>().Ignore(candidate => candidate.Revisions);
            modelBuilder.Entity<AssetReference>()
                .HasOne(candidate => candidate.Content)
                .WithMany()
                .HasForeignKey(candidate => candidate.AssetContentId);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<EntityBase<Guid>>()
                         .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                entry.Entity.Version++;
            }
            return base.SaveChangesAsync(cancellationToken);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
