using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using GameGuild.Projects;
using GameGuild.TestingLab;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class TestingEventPostgreSqlConcurrencyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("testing_event_races")
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
    public async Task ConcurrentApprovals_ForLastProjectSlot_AllowExactlyOneApplication()
    {
        var tenantId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var manager = new User
        {
            Id = managerId,
            Email = $"{managerId:N}@example.com",
            Name = "Testing Lab manager",
            IsActive = true
        };
        var testingEvent = TestingEvent.Create(
            "Capacity race",
            TestingEventMode.Online,
            managerId,
            SystemClock.UtcNow.AddDays(-2),
            SystemClock.UtcNow.AddDays(1),
            SystemClock.UtcNow.AddDays(2),
            SystemClock.UtcNow.AddDays(3),
            true,
            TestingEventApprovalMode.ManagerOnly,
            tenantId);
        testingEvent.OpenApplications();
        var slot = TestingEventSlot.Create(
            testingEvent.Id,
            TestingEventMode.Online,
            SystemClock.UtcNow.AddDays(2),
            SystemClock.UtcNow.AddDays(2).AddHours(2),
            null,
            1,
            null,
            null,
            "https://meet.example.com/race",
            tenantId);
        var firstProject = NewProject(tenantId, managerId);
        var secondProject = NewProject(tenantId, managerId);
        var firstApplication = TestingProjectApplication.Submit(
            testingEvent.Id, firstProject.Id, null, managerId, null, tenantId);
        var secondApplication = TestingProjectApplication.Submit(
            testingEvent.Id, secondProject.Id, null, managerId, null, tenantId);
        await SeedAsync(manager, testingEvent, slot, firstProject, secondProject, firstApplication, secondApplication);
        var actor = ActorAccessor(managerId, tenantId);

        await using var gateContext = new ApplicationDbContext(_options);
        await using var gate = await new ProjectLifecycleLock(gateContext).AcquireAsync(slot.Id);
        await using var observer = new NpgsqlConnection(_container.GetConnectionString());
        await observer.OpenAsync();

        var firstTask = ApproveAsync(firstApplication.Id, slot.Id, actor);
        var secondTask = ApproveAsync(secondApplication.Id, slot.Id, actor);
        await WaitForWaitingAdvisoryLocksAsync(observer, 2);
        await gate.CommitAsync();
        var results = await Task.WhenAll(firstTask, secondTask);

        results.Count(result => result.IsSuccess).Should().Be(1);
        results.Count(result => result.IsFailure && result.Error.Type == ErrorType.Conflict).Should().Be(1);
        await using var verify = new ApplicationDbContext(_options);
        (await verify.Set<TestingProjectApplication>().CountAsync(application =>
            application.AssignedSlotId == slot.Id &&
            application.Status == TestingApplicationStatus.Approved &&
            application.DeletedAt == null)).Should().Be(1);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private async Task<Result<TestingProjectApplicationProjection>> ApproveAsync(
        Guid applicationId,
        Guid slotId,
        IActorContextAccessor actorAccessor)
    {
        await using var context = new ApplicationDbContext(_options);
        return await new TestingApplicationHandlers(
                context,
                actorAccessor,
                new AllowAllProjectAuthorizationService(),
                NullLogger<TestingApplicationHandlers>.Instance,
                new ProjectLifecycleLock(context))
            .Handle(new ApproveTestingProjectApplicationCommand(applicationId, slotId, "Capacity test"), default);
    }

    private async Task SeedAsync(params object[] entities)
    {
        await using var context = new ApplicationDbContext(_options);
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    private static Project NewProject(Guid tenantId, Guid ownerId) => new()
    {
        Title = $"Race project {Guid.NewGuid():N}",
        Slug = $"race-project-{Guid.NewGuid():N}",
        TenantId = tenantId,
        CreatedById = ownerId,
        Status = ContentStatus.Draft,
        Visibility = ContentVisibility.Private
    };

    private static IActorContextAccessor ActorAccessor(Guid actorId, Guid tenantId)
    {
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(ActorContextBuilder.ForUser(actorId).WithTenantId(tenantId).Build());
        return accessor;
    }

    private static async Task WaitForWaitingAdvisoryLocksAsync(NpgsqlConnection connection, int minimumCount)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
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