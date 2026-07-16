using GameGuild.Projects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabProjectLifecycleTests
{
    [Fact]
    public async Task Delete_ShouldDeactivateProjectLinksAndRecalculateAffectedSessionCounts()
    {
        await using var context = new LifecycleContext(
            new DbContextOptionsBuilder<LifecycleContext>()
                .UseInMemoryDatabase($"testing-lifecycle-{Guid.NewGuid():N}")
                .Options);
        var project = NewProject("deleted");
        var remainingProject = NewProject("remaining");
        var firstSession = new TestingSession { RegisteredProjectCount = 99 };
        var secondSession = new TestingSession { RegisteredProjectCount = 99 };
        var targetLinks = new[]
        {
            NewLink(firstSession.Id, project.Id),
            NewLink(secondSession.Id, project.Id)
        };
        context.AddRange(project, remainingProject, firstSession, secondSession);
        context.AddRange(targetLinks);
        context.Add(NewLink(firstSession.Id, remainingProject.Id));
        context.Add(new SessionProject
        {
            SessionId = firstSession.Id,
            ProjectId = Guid.NewGuid(),
            RegisteredById = Guid.NewGuid(),
            IsActive = false
        });
        await context.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddScoped<IApplicationDbContext>(_ => context);
        services.AddTestingLabModule(new ConfigurationBuilder().Build());
        await using var provider = services.BuildServiceProvider();
        var coordinator = new ProjectLifecycleCoordinator(
            context,
            provider.GetServices<IProjectLifecycleParticipant>());

        var deleted = await coordinator.DeleteAsync(project.Id, softDelete: true);

        deleted.Should().BeTrue();
        targetLinks.Should().OnlyContain(link => !link.IsActive && link.DeletedAt != null);
        firstSession.RegisteredProjectCount.Should().Be(1);
        secondSession.RegisteredProjectCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_ShouldSoftDeleteActiveProjectTestingRequestsOnly()
    {
        await using var context = new LifecycleContext(
            new DbContextOptionsBuilder<LifecycleContext>()
                .UseInMemoryDatabase($"testing-request-lifecycle-{Guid.NewGuid():N}")
                .Options);
        var project = NewProject("request-deleted");
        var remainingProject = NewProject("request-remaining");
        var projectVersion = new ProjectVersion { Id = Guid.NewGuid(), ProjectId = project.Id, VersionNumber = "1.0" };
        var remainingVersion = new ProjectVersion { Id = Guid.NewGuid(), ProjectId = remainingProject.Id, VersionNumber = "1.0" };
        var activeRequest = NewRequest(projectVersion.Id);
        var alreadyDeletedRequest = NewRequest(projectVersion.Id);
        alreadyDeletedRequest.DeletedAt = DateTime.UtcNow.AddDays(-1);
        var remainingRequest = NewRequest(remainingVersion.Id);
        var standaloneRequest = NewRequest(projectVersionId: null);
        context.AddRange(
            project,
            remainingProject,
            projectVersion,
            remainingVersion,
            activeRequest,
            alreadyDeletedRequest,
            remainingRequest,
            standaloneRequest);
        await context.SaveChangesAsync();

        var coordinator = new ProjectLifecycleCoordinator(
            context,
            [new TestingLabProjectLifecycleParticipant(context)]);

        var deleted = await coordinator.DeleteAsync(project.Id, softDelete: true);

        deleted.Should().BeTrue();
        activeRequest.DeletedAt.Should().NotBeNull();
        alreadyDeletedRequest.DeletedAt.Should().NotBeNull();
        remainingRequest.DeletedAt.Should().BeNull();
        standaloneRequest.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task HardDelete_ShouldRemoveSessionLinksRecalculateCountsAndPreserveTestingRequestHistory()
    {
        await using var context = new LifecycleContext(
            new DbContextOptionsBuilder<LifecycleContext>()
                .UseInMemoryDatabase($"testing-hard-delete-{Guid.NewGuid():N}")
                .Options);
        var project = NewProject("hard-deleted");
        var remainingProject = NewProject("hard-delete-remaining");
        var projectVersion = new ProjectVersion
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNumber = "1.0"
        };
        var request = NewRequest(projectVersion.Id);
        var session = new TestingSession { RegisteredProjectCount = 99 };
        var activeTarget = NewLink(session.Id, project.Id);
        var historicalTarget = NewLink(session.Id, project.Id);
        historicalTarget.IsActive = false;
        historicalTarget.DeletedAt = SystemClock.UtcNow.AddDays(-1);
        var remainingLink = NewLink(session.Id, remainingProject.Id);
        context.AddRange(
            project,
            remainingProject,
            projectVersion,
            request,
            session,
            activeTarget,
            historicalTarget,
            remainingLink);
        await context.SaveChangesAsync();

        var deleted = await new ProjectLifecycleCoordinator(
                context,
                [new TestingLabProjectLifecycleParticipant(context)])
            .DeleteAsync(project.Id, softDelete: false);

        deleted.Should().BeTrue();
        (await context.Set<SessionProject>().IgnoreQueryFilters()
            .AnyAsync(link => link.ProjectId == project.Id)).Should().BeFalse();
        session.RegisteredProjectCount.Should().Be(1);
        request.DeletedAt.Should().BeNull();
    }

    private static Project NewProject(string suffix) => new()
    {
        Id = Guid.NewGuid(),
        Title = $"Project {suffix}",
        Slug = $"project-{suffix}-{Guid.NewGuid():N}",
        Status = ContentStatus.Draft
    };

    private static SessionProject NewLink(Guid sessionId, Guid projectId) => new()
    {
        SessionId = sessionId,
        ProjectId = projectId,
        RegisteredById = Guid.NewGuid(),
        IsActive = true
    };

    private static TestingRequest NewRequest(Guid? projectVersionId) => new()
    {
        ProjectVersionId = projectVersionId,
        Title = "Lifecycle request",
        InstructionsType = InstructionType.Text,
        StartDate = DateTime.UtcNow,
        EndDate = DateTime.UtcNow.AddDays(1),
        CreatedById = Guid.NewGuid()
    };

    private sealed class LifecycleContext(DbContextOptions<LifecycleContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<TestingSession> TestingSessions => Set<TestingSession>();
        public DbSet<SessionProject> SessionProjects => Set<SessionProject>();
        public DbSet<ProjectVersion> ProjectVersions => Set<ProjectVersion>();
        public DbSet<TestingRequest> TestingRequests => Set<TestingRequest>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException("The lifecycle coordinator uses its explicit in-memory fallback.");
    }
}
