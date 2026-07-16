using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Commerce.Products;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using GameGuild.LaunchPad;
using GameGuild.Projects;
using GameGuild.TestingLab;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace GameGuild.API.UnitTests.Database;

public sealed class ProjectChannelPostgreSqlRaceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("project_channel_races")
        .WithUsername("test")
        .WithPassword("test")
        .WithCleanUp(true)
        .Build();

    private DbContextOptions<ApplicationDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        await using var context = new ApplicationDbContext(_options);
        await context.Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task ConcurrentDeleteAndStoreLink_CannotLeaveActiveLinkOnDeletedProject()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var project = NewProject(tenantId, ContentStatus.Published, ContentVisibility.Public);
        var product = Product.Create("Race product", creatorId: actorId, tenantId: tenantId);
        product.IsPublished = true;
        await SeedAsync(NewUser(actorId), project, product);
        var actorAccessor = ActorAccessor(actorId, tenantId);

        await using var gateContext = new ApplicationDbContext(_options);
        await using var gate = await new ProjectLifecycleLock(gateContext).AcquireAsync(project.Id);
        await using var observer = new NpgsqlConnection(_container.GetConnectionString());
        await observer.OpenAsync();

        var linkTask = LinkStoreAsync(project.Id, product.Id, actorAccessor);
        await WaitForWaitingAdvisoryLocksAsync(observer, 1);
        var deleteTask = DeleteAsync(project.Id);
        await WaitForWaitingAdvisoryLocksAsync(observer, 2);

        await gate.CommitAsync();
        (await linkTask).IsSuccess.Should().BeTrue();
        (await deleteTask).Should().BeTrue();
        await using var verify = new ApplicationDbContext(_options);
        (await verify.Set<Project>().IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == project.Id))
            .DeletedAt.Should().NotBeNull();
        (await verify.Set<ProjectStoreProduct>().AnyAsync(link =>
            link.ProjectId == project.Id && link.DeletedAt == null)).Should().BeFalse();
    }

    [Fact]
    public async Task ConcurrentDeleteAndSessionLink_CannotLeaveActiveLinkOnDeletedProject()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var user = NewUser(actorId);
        var project = NewProject(tenantId, ContentStatus.Draft, ContentVisibility.Private);
        var request = new TestingRequest
        {
            Title = "Race request",
            InstructionsType = InstructionType.Text,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedById = actorId,
            TenantId = tenantId
        };
        var location = new TestingLocation { Name = "Race lab", TenantId = tenantId };
        var session = new TestingSession
        {
            TestingRequestId = request.Id,
            LocationId = location.Id,
            SessionName = "Race session",
            SessionDate = DateTime.UtcNow,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            MaxTesters = 8,
            ManagerId = actorId,
            ManagerUserId = actorId,
            CreatedById = actorId,
            TenantId = tenantId
        };
        await SeedAsync(user, project, request, location, session);
        var actorAccessor = ActorAccessor(actorId, tenantId);

        await using var gateContext = new ApplicationDbContext(_options);
        await using var gate = await new ProjectLifecycleLock(gateContext).AcquireAsync(project.Id);
        await using var observer = new NpgsqlConnection(_container.GetConnectionString());
        await observer.OpenAsync();

        var linkTask = LinkSessionAsync(session.Id, project.Id, actorAccessor);
        await WaitForWaitingAdvisoryLocksAsync(observer, 1);
        var deleteTask = DeleteAsync(project.Id);
        await WaitForWaitingAdvisoryLocksAsync(observer, 2);

        await gate.CommitAsync();
        (await linkTask).IsSuccess.Should().BeTrue();
        (await deleteTask).Should().BeTrue();
        await using var verify = new ApplicationDbContext(_options);
        (await verify.Set<SessionProject>().AnyAsync(link =>
            link.ProjectId == project.Id && link.IsActive && link.DeletedAt == null)).Should().BeFalse();
        (await verify.Set<TestingSession>().SingleAsync(candidate => candidate.Id == session.Id))
            .RegisteredProjectCount.Should().Be(0);
    }

    [Fact]
    public async Task ConcurrentDeleteAndLaunchPlanCreate_CannotLeaveActivePlanOnDeletedProject()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var project = NewProject(tenantId, ContentStatus.Draft, ContentVisibility.Private);
        await SeedAsync(NewUser(actorId), project);
        var actorAccessor = ActorAccessor(actorId, tenantId);

        await using var gateContext = new ApplicationDbContext(_options);
        await using var gate = await new ProjectLifecycleLock(gateContext).AcquireAsync(project.Id);
        await using var observer = new NpgsqlConnection(_container.GetConnectionString());
        await observer.OpenAsync();

        var createTask = CreateLaunchPlanAsync(project.Id, actorAccessor);
        await WaitForWaitingAdvisoryLocksAsync(observer, 1);
        var deleteTask = DeleteAsync(project.Id);
        await WaitForWaitingAdvisoryLocksAsync(observer, 2);

        await gate.CommitAsync();
        (await createTask).IsSuccess.Should().BeTrue();
        (await deleteTask).Should().BeTrue();
        await using var verify = new ApplicationDbContext(_options);
        (await verify.Set<LaunchPlan>().AnyAsync(plan =>
            plan.ProjectId == project.Id && plan.DeletedAt == null)).Should().BeFalse();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private async Task<Result<ProjectStoreProductProjection>> LinkStoreAsync(
        Guid projectId,
        Guid productId,
        IActorContextAccessor actorAccessor)
    {
        await using var context = new ApplicationDbContext(_options);
        return await new ProjectStoreProductHandlers(
                context,
                actorAccessor,
                new ProjectChannelAvailabilityService(context),
                new AllowAllProjectAuthorizationService(),
                NullLogger<ProjectStoreProductHandlers>.Instance,
                new ProjectLifecycleLock(context))
            .Handle(new LinkProjectStoreProductCommand(projectId, productId), default);
    }

    private async Task<Result<SessionProjectProjection>> LinkSessionAsync(
        Guid sessionId,
        Guid projectId,
        IActorContextAccessor actorAccessor)
    {
        await using var context = new ApplicationDbContext(_options);
        return await new SessionProjectHandlers(
                context,
                actorAccessor,
                new ProjectChannelAvailabilityService(context),
                new AllowAllProjectAuthorizationService(),
                NullLogger<SessionProjectHandlers>.Instance,
                new ProjectLifecycleLock(context))
            .Handle(new LinkSessionProjectCommand(sessionId, projectId), default);
    }

    private async Task<Result<LaunchPlan>> CreateLaunchPlanAsync(
        Guid projectId,
        IActorContextAccessor actorAccessor)
    {
        await using var context = new ApplicationDbContext(_options);
        return await new LaunchPadHandlers(
                context,
                actorAccessor,
                new ProjectChannelAvailabilityService(context),
                new AllowAllProjectAuthorizationService(),
                NullLogger<LaunchPadHandlers>.Instance,
                new ProjectLifecycleLock(context))
            .Handle(new CreateLaunchPlanCommand { ProjectId = projectId, Name = "Race plan" }, default);
    }

    private async Task<bool> DeleteAsync(Guid projectId)
    {
        await using var context = new ApplicationDbContext(_options);
        return await new ProjectLifecycleCoordinator(
                context,
                [
                    new ProjectStoreProductLifecycleParticipant(context),
                    new TestingLabProjectLifecycleParticipant(context),
                    new LaunchPadProjectLifecycleParticipant(context)
                ],
                new ProjectLifecycleLock(context))
            .DeleteAsync(projectId, softDelete: true);
    }

    private async Task SeedAsync(params object[] entities)
    {
        await using var context = new ApplicationDbContext(_options);
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    private static Project NewProject(
        Guid tenantId,
        ContentStatus status,
        ContentVisibility visibility) => new()
    {
        Title = "Race project",
        Slug = $"race-project-{Guid.NewGuid():N}",
        TenantId = tenantId,
        Status = status,
        Visibility = visibility
    };

    private static User NewUser(Guid userId) => new()
    {
        Id = userId,
        Email = $"{userId:N}@example.com",
        Name = "Project channel race actor",
        IsActive = true
    };

    private static IActorContextAccessor ActorAccessor(Guid actorId, Guid tenantId)
    {
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(ActorContextBuilder.ForUser(actorId).WithTenantId(tenantId).Build());
        return accessor;
    }

    private static async Task WaitForWaitingAdvisoryLocksAsync(
        NpgsqlConnection connection,
        int minimumCount)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            await using var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM pg_locks WHERE locktype = 'advisory' AND NOT granted AND database = (SELECT oid FROM pg_database WHERE datname = current_database())",
                connection);
            if (Convert.ToInt32(await command.ExecuteScalarAsync()) >= minimumCount) return;
            await Task.Delay(25);
        }

        throw new TimeoutException($"Timed out waiting for {minimumCount} advisory lock waiters.");
    }

    private sealed class AllowAllProjectAuthorizationService : IProjectAuthorizationService
    {
        public Task<bool> IsActorActiveTenantMemberAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> HasPermissionAsync(
            Guid projectId,
            PermissionType permission,
            CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
