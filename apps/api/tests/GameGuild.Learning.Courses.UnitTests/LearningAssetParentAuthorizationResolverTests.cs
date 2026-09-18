using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public sealed class LearningAssetParentAuthorizationResolverTests
{
    [Theory]
    [InlineData("Program")]
    [InlineData("Course")]
    [InlineData("programs")]
    [InlineData("courses")]
    [InlineData("ProgramContent")]
    [InlineData("lessons")]
    public void Supports_RecognizesLearningParentAliases(string resourceType)
    {
        using var context = CreateContext();
        var resolver = CreateResolver(context, Guid.NewGuid(), Guid.NewGuid());

        resolver.Supports(resourceType).Should().BeTrue();
    }

    [Fact]
    public async Task CanReadAsync_AllowsActiveLearnerForContentInCurrentTenant()
    {
        var tenantId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var program = new Program { Id = Guid.NewGuid(), TenantId = tenantId };
        var content = new ProgramContent
        {
            Id = Guid.NewGuid(),
            ProgramId = program.Id,
            TenantId = tenantId,
            Title = "Portable media lesson",
            Slug = "portable-media-lesson",
        };
        var enrollment = new ProgramEnrollment
        {
            Id = Guid.NewGuid(),
            ProgramId = program.Id,
            UserId = learnerId,
            TenantId = tenantId,
            EnrollmentStatus = EnrollmentStatus.Active,
        };
        await using var context = CreateContext();
        context.AddRange(program, content, enrollment);
        await context.SaveChangesAsync();
        var resolver = CreateResolver(context, learnerId, tenantId);

        var result = await resolver.CanReadAsync(content.Id, learnerId, tenantId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanManageAsync_AllowsEditorAndRejectsCrossTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var program = new Program { Id = Guid.NewGuid(), TenantId = tenantId };
        var content = new ProgramContent
        {
            Id = Guid.NewGuid(),
            ProgramId = program.Id,
            TenantId = tenantId,
            Title = "Asset-backed lesson",
            Slug = "asset-backed-lesson",
        };
        await using var context = CreateContext();
        context.AddRange(program, content);
        await context.SaveChangesAsync();
        var permissions = new Mock<IPermissionQueryService>();
        permissions.Setup(service => service.HasTenantPermissionAsync(
                editorId,
                tenantId,
                $"Program.{program.Id}.Edit",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var resolver = CreateResolver(context, editorId, tenantId, permissions);

        (await resolver.CanManageAsync(content.Id, editorId, tenantId)).Should().BeTrue();
        (await resolver.CanManageAsync(content.Id, editorId, Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task CanReadAsync_RejectsSpoofedActorIdentity()
    {
        var tenantId = Guid.NewGuid();
        var actualActorId = Guid.NewGuid();
        var requestedUserId = Guid.NewGuid();
        var program = new Program
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatorId = requestedUserId,
        };
        await using var context = CreateContext();
        context.Add(program);
        await context.SaveChangesAsync();
        var resolver = CreateResolver(context, actualActorId, tenantId);

        var result = await resolver.CanReadAsync(program.Id, requestedUserId, tenantId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanReadAsync_RejectsUnauthenticatedActor()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        await using var context = CreateContext();
        var resolver = CreateResolver(
            context,
            actorId,
            tenantId,
            isAuthenticated: false);

        var result = await resolver.CanReadAsync(Guid.NewGuid(), actorId, tenantId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanReadAsync_RejectsMissingRequestedTenant()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        await using var context = CreateContext();
        var resolver = CreateResolver(context, actorId, tenantId);

        var result = await resolver.CanReadAsync(Guid.NewGuid(), actorId, null);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanReadAsync_RejectsActorWithoutGuidSubject()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        await using var context = CreateContext();
        var resolver = CreateResolver(
            context,
            actorId,
            tenantId,
            actorSubjectId: "not-a-guid");

        var result = await resolver.CanReadAsync(Guid.NewGuid(), actorId, tenantId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanReadAsync_RejectsActorWithoutTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        await using var context = CreateContext();
        var resolver = CreateResolver(
            context,
            actorId,
            tenantId,
            hasActorTenant: false);

        var result = await resolver.CanReadAsync(Guid.NewGuid(), actorId, tenantId);

        result.Should().BeFalse();
    }

    private static LearningAssetParentAuthorizationResolver CreateResolver(
        TestDbContext context,
        Guid actorId,
        Guid tenantId,
        Mock<IPermissionQueryService>? permissions = null,
        bool isAuthenticated = true,
        string? actorSubjectId = null,
        bool hasActorTenant = true)
    {
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = actorSubjectId ?? actorId.ToString(),
            TenantId = hasActorTenant ? tenantId : null,
            IsAuthenticated = isAuthenticated,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
        });
        return new LearningAssetParentAuthorizationResolver(
            context,
            actor.Object,
            permissions?.Object ?? Mock.Of<IPermissionQueryService>());
    }

    private static TestDbContext CreateContext() => new(
        new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Program>(entity =>
            {
                entity.Ignore(program => program.ProgramContents);
                entity.Ignore(program => program.ProgramUsers);
                entity.Ignore(program => program.ProgramRatings);
                entity.Ignore(program => program.ProgramWishlists);
            });
            modelBuilder.Entity<ProgramContent>(entity =>
            {
                entity.Ignore(content => content.Program);
                entity.Ignore(content => content.Parent);
                entity.Ignore(content => content.Children);
                entity.Ignore(content => content.ContentInteractions);
            });
            modelBuilder.Entity<ProgramEnrollment>(entity =>
            {
                entity.Ignore(enrollment => enrollment.Program);
                entity.Ignore(enrollment => enrollment.User);
            });
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
