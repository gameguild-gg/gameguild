using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingPublicEventQueryTests : IDisposable
{
    private readonly TestContext _context;
    private readonly ActorContextAccessor _actorAccessor = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();

    public TestingPublicEventQueryTests()
    {
        _context = new TestContext(new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase($"testing-public-events-{Guid.NewGuid():N}")
            .Options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task PublicDirectory_AllowsAnonymousAndOnlyReturnsPublicLifecycleStates()
    {
        var draft = AddEvent("Draft");
        var open = AddEvent("Open");
        open.OpenApplications();
        var closed = AddEvent("Closed");
        closed.OpenApplications();
        closed.CloseApplications();
        var scheduled = AddEvent("Scheduled");
        scheduled.OpenApplications();
        scheduled.CloseApplications();
        scheduled.Schedule();
        var active = AddEvent("Active");
        active.OpenApplications();
        active.CloseApplications();
        active.Schedule();
        active.Activate();
        var completed = AddEvent("Completed");
        completed.OpenApplications();
        completed.CloseApplications();
        completed.Schedule();
        completed.Activate();
        completed.Complete();
        var cancelled = AddEvent("Cancelled");
        cancelled.Cancel("Venue unavailable");
        await _context.SaveChangesAsync();

        _actorAccessor.SetActorContext(ActorContext.Anonymous);
        var result = await CreateHandler().Handle(new GetPublicTestingEventsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(testingEvent => testingEvent.Id).Should().BeEquivalentTo(
            new[]
            {
                open.Id,
                closed.Id,
                scheduled.Id,
                active.Id
            });
        result.Value.Should().NotContain(testingEvent =>
            testingEvent.Id == draft.Id ||
            testingEvent.Id == completed.Id ||
            testingEvent.Id == cancelled.Id);
    }

    [Fact]
    public async Task PublicDetail_ReportsCapacityWithoutExposingOnlineMeetingUrl()
    {
        var testingEvent = AddEvent("Public event");
        testingEvent.OpenApplications();
        var slot = TestingEventSlot.Create(
            testingEvent.Id,
            TestingEventMode.Online,
            testingEvent.StartsAt,
            testingEvent.StartsAt.AddHours(2),
            2,
            1,
            null,
            null,
            "https://private.example.com/meeting",
            _tenantId);
        _context.Add(slot);
        _context.Add(TestingSlotRegistration.Register(
            testingEvent.Id,
            slot.Id,
            Guid.NewGuid(),
            null,
            _tenantId));
        await _context.SaveChangesAsync();

        _actorAccessor.SetActorContext(ActorContext.Anonymous);
        var result = await CreateHandler().Handle(new GetPublicTestingEventQuery(testingEvent.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slots.Should().ContainSingle();
        var publicSlot = result.Value.Slots.Single();
        publicSlot.RegisteredTesterCount.Should().Be(1);
        publicSlot.AvailableTesterCount.Should().Be(1);
        typeof(PublicTestingEventSlotProjection).GetProperty("MeetingUrl").Should().BeNull();
    }

    [Fact]
    public async Task PublicDetail_ReturnsNotFoundForDraftEvent()
    {
        var testingEvent = AddEvent("Private draft");
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new GetPublicTestingEventQuery(testingEvent.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task PublicDirectory_WhenAuthenticated_OnlyReturnsEventsFromTheActiveTenant()
    {
        AddActiveActor(_managerId, _tenantId);
        var visible = AddEvent("Visible event");
        visible.OpenApplications();
        var otherTenant = TestingEvent.Create(
            "Other tenant event",
            TestingEventMode.Online,
            Guid.NewGuid(),
            SystemClock.UtcNow.AddDays(-2),
            SystemClock.UtcNow.AddDays(1),
            SystemClock.UtcNow.AddDays(2),
            SystemClock.UtcNow.AddDays(3),
            true,
            TestingEventApprovalMode.ManagerOnly,
            Guid.NewGuid());
        otherTenant.OpenApplications();
        _context.Add(otherTenant);
        await _context.SaveChangesAsync();
        _actorAccessor.SetActorContext(
            ActorContextBuilder.ForUser(_managerId).WithTenantId(_tenantId).Build());

        var result = await CreateHandler().Handle(new GetPublicTestingEventsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(testingEvent => testingEvent.Id == visible.Id);
        result.Value.Should().NotContain(testingEvent => testingEvent.Id == otherTenant.Id);
    }
    private TestingEventHandlers CreateHandler() => new(_context, _actorAccessor);
    private void AddActiveActor(Guid userId, Guid tenantId)
    {
        _context.Add(new User
        {
            Id = userId,
            Email = $"{userId:N}@example.com",
            Name = "Testing Lab manager",
            IsActive = true
        });
        _context.Add(new TenantMember
        {
            UserId = userId,
            TenantId = tenantId,
            Role = "Admin",
            IsActive = true
        });
    }

    private TestingEvent AddEvent(string name)
    {
        var applicationsOpenAt = SystemClock.UtcNow.AddDays(-2);
        var applicationsCloseAt = SystemClock.UtcNow.AddDays(1);
        var testingEvent = TestingEvent.Create(
            name,
            TestingEventMode.Online,
            _managerId,
            applicationsOpenAt,
            applicationsCloseAt,
            applicationsCloseAt.AddDays(1),
            applicationsCloseAt.AddDays(2),
            true,
            TestingEventApprovalMode.ManagerOnly,
            _tenantId);
        _context.Add(testingEvent);
        return testingEvent;
    }

    private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options), IApplicationDbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<TenantMember> TenantMembers => Set<TenantMember>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectVersion> ProjectVersions => Set<ProjectVersion>();
        public DbSet<ProjectCollaborator> ProjectCollaborators => Set<ProjectCollaborator>();
        public DbSet<TestingEvent> TestingEvents => Set<TestingEvent>();
        public DbSet<TestingEventSlot> TestingEventSlots => Set<TestingEventSlot>();
        public DbSet<TestingProjectApplication> TestingProjectApplications => Set<TestingProjectApplication>();
        public DbSet<TestingCommitteeMember> TestingCommitteeMembers => Set<TestingCommitteeMember>();
        public DbSet<TestingApplicationVote> TestingApplicationVotes => Set<TestingApplicationVote>();
        public DbSet<TestingSession> TestingSessions => Set<TestingSession>();
        public DbSet<SessionRegistration> SessionRegistrations => Set<SessionRegistration>();
        public DbSet<TestingSlotRegistration> TestingSlotRegistrations => Set<TestingSlotRegistration>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
