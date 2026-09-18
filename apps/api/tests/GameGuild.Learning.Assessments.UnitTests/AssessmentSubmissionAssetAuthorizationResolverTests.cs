using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class AssessmentSubmissionAssetAuthorizationResolverTests
{
    [Fact]
    public void Supports_AcceptsCanonicalAliasesOnly()
    {
        using var context = CreateContext();
        var resolver = CreateResolver(context, Guid.NewGuid(), Guid.NewGuid());

        resolver.Supports(nameof(AssessmentSubmission)).Should().BeTrue();
        resolver.Supports("SUBMISSIONS").Should().BeTrue();
        resolver.Supports("assessment").Should().BeFalse();
    }

    [Fact]
    public async Task Authorization_DeniesUnauthenticatedActorAndMissingRequestedTenant()
    {
        await using var context = CreateContext();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var unauthenticated = CreateResolver(
            context,
            new ActorContext
            {
                ActorKind = ActorKind.User,
                SubjectId = actorId.ToString(),
                TenantId = tenantId,
                IsAuthenticated = false,
                Roles = new HashSet<string>(),
                Permissions = new HashSet<string>(),
            });
        var authenticated = CreateResolver(context, actorId, tenantId);

        (await unauthenticated.CanReadAsync(Guid.NewGuid(), actorId, tenantId)).Should().BeFalse();
        (await authenticated.CanManageAsync(Guid.NewGuid(), actorId, null)).Should().BeFalse();
    }

    [Fact]
    public async Task Authorization_DeniesInvalidActorSubjectAndMissingActorTenant()
    {
        await using var context = CreateContext();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var invalidSubject = CreateResolver(
            context,
            new ActorContext
            {
                ActorKind = ActorKind.User,
                SubjectId = "not-a-guid",
                TenantId = tenantId,
                IsAuthenticated = true,
                Roles = new HashSet<string>(),
                Permissions = new HashSet<string>(),
            });
        var missingActorTenant = CreateResolver(
            context,
            new ActorContext
            {
                ActorKind = ActorKind.User,
                SubjectId = actorId.ToString(),
                TenantId = null,
                IsAuthenticated = true,
                Roles = new HashSet<string>(),
                Permissions = new HashSet<string>(),
            });

        (await invalidSubject.CanReadAsync(Guid.NewGuid(), actorId, tenantId)).Should().BeFalse();
        (await missingActorTenant.CanManageAsync(Guid.NewGuid(), actorId, tenantId)).Should().BeFalse();
    }

    [Fact]
    public async Task CanManageAsync_AllowsOnlyOwnerOfInProgressSubmissionInCurrentTenant()
    {
        var tenantId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var program = new Program { Id = Guid.NewGuid(), TenantId = tenantId };
        var assessment = Assessment.Create(program.Id, "Upload", AssessmentType.Assignment, Score(100));
        assessment.TenantId = tenantId;
        var submission = AssessmentSubmission.Start(assessment.Id, Guid.NewGuid(), learnerId, 1);
        submission.TenantId = tenantId;
        await using var context = CreateContext();
        context.AddRange(program, assessment, submission);
        await context.SaveChangesAsync();
        var resolver = CreateResolver(context, learnerId, tenantId);

        (await resolver.CanManageAsync(submission.Id, learnerId, tenantId)).Should().BeTrue();
        (await resolver.CanManageAsync(assessment.Id, learnerId, tenantId)).Should().BeFalse(
            "the asset parent must be the concrete submission, not the assessment");
        (await resolver.CanManageAsync(submission.Id, learnerId, Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task CanReadAsync_AllowsCourseReviewerButNotUnrelatedTenantMember()
    {
        var tenantId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var program = new Program { Id = Guid.NewGuid(), TenantId = tenantId };
        var assessment = Assessment.Create(program.Id, "Upload", AssessmentType.Assignment, Score(100));
        assessment.TenantId = tenantId;
        var submission = AssessmentSubmission.Start(assessment.Id, Guid.NewGuid(), learnerId, 1);
        submission.TenantId = tenantId;
        await using var context = CreateContext();
        context.AddRange(program, assessment, submission);
        await context.SaveChangesAsync();
        var permissions = new Mock<IPermissionQueryService>();
        permissions.Setup(service => service.HasTenantPermissionAsync(
                reviewerId,
                tenantId,
                $"Program.{program.Id}.Review",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var reviewer = CreateResolver(context, reviewerId, tenantId, permissions);
        var unrelated = CreateResolver(context, Guid.NewGuid(), tenantId);

        (await reviewer.CanReadAsync(submission.Id, reviewerId, tenantId)).Should().BeTrue();
        (await unrelated.CanReadAsync(submission.Id, Guid.NewGuid(), tenantId)).Should().BeFalse();
    }

    [Fact]
    public async Task CanReadAndManageAsync_DeniesDeletedSubmission()
    {
        var tenantId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var program = new Program { Id = Guid.NewGuid(), TenantId = tenantId };
        var assessment = Assessment.Create(program.Id, "Upload", AssessmentType.Assignment, Score(100));
        assessment.TenantId = tenantId;
        var submission = AssessmentSubmission.Start(assessment.Id, Guid.NewGuid(), learnerId, 1);
        submission.TenantId = tenantId;
        await using var context = CreateContext();
        context.AddRange(program, assessment, submission);
        await context.SaveChangesAsync();
        submission.Version = 1;
        submission.SoftDelete();
        await context.SaveChangesAsync();
        var resolver = CreateResolver(context, learnerId, tenantId);

        (await resolver.CanReadAsync(submission.Id, learnerId, tenantId)).Should().BeFalse();
        (await resolver.CanManageAsync(submission.Id, learnerId, tenantId)).Should().BeFalse();
    }

    [Theory]
    [InlineData("Edit")]
    [InlineData("Publish")]
    public async Task CanReadAsync_AllowsEachManagementPermission(string grantedPermission)
    {
        var tenantId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var program = new Program { Id = Guid.NewGuid(), TenantId = tenantId };
        var assessment = Assessment.Create(program.Id, "Upload", AssessmentType.Assignment, Score(100));
        assessment.TenantId = tenantId;
        var submission = AssessmentSubmission.Start(assessment.Id, Guid.NewGuid(), learnerId, 1);
        submission.TenantId = tenantId;
        await using var context = CreateContext();
        context.AddRange(program, assessment, submission);
        await context.SaveChangesAsync();
        var permissions = new Mock<IPermissionQueryService>();
        permissions.Setup(service => service.HasTenantPermissionAsync(
                reviewerId,
                tenantId,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid? _, string permission, CancellationToken _) =>
                permission.EndsWith($".{grantedPermission}", StringComparison.Ordinal));
        var resolver = CreateResolver(context, reviewerId, tenantId, permissions);

        (await resolver.CanReadAsync(submission.Id, reviewerId, tenantId)).Should().BeTrue();
    }

    [Fact]
    public async Task CanReadAsync_DeniesAuthoritativeActorWithoutAnyPermission()
    {
        var tenantId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var program = new Program { Id = Guid.NewGuid(), TenantId = tenantId };
        var assessment = Assessment.Create(program.Id, "Upload", AssessmentType.Assignment, Score(100));
        assessment.TenantId = tenantId;
        var submission = AssessmentSubmission.Start(assessment.Id, Guid.NewGuid(), learnerId, 1);
        submission.TenantId = tenantId;
        await using var context = CreateContext();
        context.AddRange(program, assessment, submission);
        await context.SaveChangesAsync();
        var resolver = CreateResolver(context, actorId, tenantId);

        (await resolver.CanReadAsync(submission.Id, actorId, tenantId)).Should().BeFalse();
    }

    private static AssessmentSubmissionAssetAuthorizationResolver CreateResolver(
        TestDbContext context,
        Guid actorId,
        Guid tenantId,
        Mock<IPermissionQueryService>? permissions = null)
    {
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = actorId.ToString(),
            TenantId = tenantId,
            IsAuthenticated = true,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
        });
        return new AssessmentSubmissionAssetAuthorizationResolver(
            context,
            actor.Object,
            permissions?.Object ?? Mock.Of<IPermissionQueryService>());
    }

    private static AssessmentSubmissionAssetAuthorizationResolver CreateResolver(
        TestDbContext context,
        ActorContext actorContext)
    {
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(accessor => accessor.ActorContext).Returns(actorContext);
        return new AssessmentSubmissionAssetAuthorizationResolver(
            context,
            actor.Object,
            Mock.Of<IPermissionQueryService>());
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
            modelBuilder.Entity<Assessment>(entity =>
            {
                entity.Ignore(item => item.AssessmentGroup);
                entity.Ignore(item => item.InteractiveVideoCues);
            });
            modelBuilder.Entity<AssessmentSubmission>();
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
