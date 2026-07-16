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

    private static Project NewProject(string suffix) => new()
    {
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

    private sealed class LifecycleContext(DbContextOptions<LifecycleContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<TestingSession> TestingSessions => Set<TestingSession>();
        public DbSet<SessionProject> SessionProjects => Set<SessionProject>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException("The lifecycle coordinator uses its explicit in-memory fallback.");
    }
}
