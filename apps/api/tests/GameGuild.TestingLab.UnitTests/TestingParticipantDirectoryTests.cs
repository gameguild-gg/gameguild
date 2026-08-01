using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingParticipantDirectoryTests : IDisposable
{
    private readonly TestContext _context;
    private readonly ActorContextAccessor _actorAccessor = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();

    public TestingParticipantDirectoryTests()
    {
        _context = new TestContext(new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase($"testing-participant-directory-{Guid.NewGuid():N}")
            .Options);
        AddActor(_managerId, _tenantId);
        _actorAccessor.SetActorContext(
            ActorContextBuilder.ForUser(_managerId).WithTenantId(_tenantId).Build());
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Directory_ReturnsTenantScopedHumanReadableRegistrations()
    {
        var tester = AddActor(Guid.NewGuid(), _tenantId, "Ada Tester", "ada@example.com");
        var testingEvent = AddEvent(_tenantId, _managerId, "Campus playtest");
        var slot = AddSlot(testingEvent, _tenantId, "North campus", "Lab 204");
        _context.Add(TestingSlotRegistration.Register(
            testingEvent.Id,
            slot.Id,
            tester.Id,
            "Accessibility testing",
            _tenantId));

        var otherTenantId = Guid.NewGuid();
        var otherManager = AddActor(Guid.NewGuid(), otherTenantId, "Other manager", "other@example.com");
        var otherEvent = AddEvent(otherTenantId, otherManager.Id, "Hidden event");
        var otherSlot = AddSlot(otherEvent, otherTenantId, null, null);
        _context.Add(TestingSlotRegistration.Register(
            otherEvent.Id,
            otherSlot.Id,
            otherManager.Id,
            null,
            otherTenantId));
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetTestingParticipantDirectoryQuery(Search: "Ada", Take: 25),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle();
        var item = result.Value.Items.Single();
        item.UserName.Should().Be("Ada Tester");
        item.UserEmail.Should().Be("ada@example.com");
        item.EventName.Should().Be("Campus playtest");
        item.CampusName.Should().Be("North campus");
        item.RoomName.Should().Be("Lab 204");
    }

    [Fact]
    public async Task Directory_ReturnsStatusTotalsBeforePagination()
    {
        var testingEvent = AddEvent(_tenantId, _managerId, "Online review");
        var slot = AddSlot(testingEvent, _tenantId, null, null);
        for (var index = 0; index < 3; index++)
        {
            var tester = AddActor(Guid.NewGuid(), _tenantId, $"Tester {index}", $"tester{index}@example.com");
            var registration = index == 2
                ? TestingSlotRegistration.Waitlist(
                    testingEvent.Id,
                    slot.Id,
                    tester.Id,
                    1,
                    null,
                    _tenantId)
                : TestingSlotRegistration.Register(
                    testingEvent.Id,
                    slot.Id,
                    tester.Id,
                    null,
                    _tenantId);
            _context.Add(registration);
        }
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetTestingParticipantDirectoryQuery(Skip: 1, Take: 1),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(3);
        result.Value.RegisteredCount.Should().Be(2);
        result.Value.WaitlistedCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle();
    }

    private TestingParticipationHandlers CreateHandler() => new(
        _context,
        _actorAccessor,
        NullLogger<TestingParticipationHandlers>.Instance);

    private User AddActor(
        Guid userId,
        Guid tenantId,
        string name = "Testing Lab manager",
        string? email = null)
    {
        var user = new User
        {
            Id = userId,
            Email = email ?? $"{userId:N}@example.com",
            Name = name,
            IsActive = true
        };
        _context.Add(user);
        _context.Add(new TenantMember
        {
            UserId = userId,
            TenantId = tenantId,
            Role = "Member",
            IsActive = true
        });
        return user;
    }

    private TestingEvent AddEvent(Guid tenantId, Guid managerId, string name)
    {
        var testingEvent = TestingEvent.Create(
            name,
            TestingEventMode.InPerson,
            managerId,
            SystemClock.UtcNow.AddDays(-2),
            SystemClock.UtcNow.AddDays(1),
            SystemClock.UtcNow.AddDays(2),
            SystemClock.UtcNow.AddDays(3),
            true,
            TestingEventApprovalMode.ManagerOnly,
            tenantId);
        _context.Add(testingEvent);
        return testingEvent;
    }

    private TestingEventSlot AddSlot(
        TestingEvent testingEvent,
        Guid tenantId,
        string? campusName,
        string? roomName)
    {
        var isInPerson = !string.IsNullOrWhiteSpace(campusName) && !string.IsNullOrWhiteSpace(roomName);
        var slot = TestingEventSlot.Create(
            testingEvent.Id,
            isInPerson ? TestingEventMode.InPerson : TestingEventMode.Online,
            testingEvent.StartsAt,
            testingEvent.StartsAt.AddHours(2),
            10,
            4,
            campusName,
            roomName,
            isInPerson ? null : "https://meet.example.com/testing",
            tenantId);
        _context.Add(slot);
        return slot;
    }

    private sealed class TestContext(DbContextOptions<TestContext> options)
        : DbContext(options), IApplicationDbContext
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
        public DbSet<TestingFeedbackObligation> TestingFeedbackObligations => Set<TestingFeedbackObligation>();

        public Task<IDbContextTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}