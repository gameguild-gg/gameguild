using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System.Reflection;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingParticipantTenantIsolationTests : IDisposable
{
    private readonly TestContext _context = new(new DbContextOptionsBuilder<TestContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Mock<IActorContextAccessor> _actors = new();

    public TestingParticipantTenantIsolationTests()
    {
        _actors.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = _actorId.ToString(),
            TenantId = _tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true,
        });
        _context.Set<User>().Add(new User
        {
            Id = _actorId,
            Email = $"{_actorId:N}@example.com",
            Name = "Tenant tester",
            IsActive = true,
        });
        _context.Set<TenantMember>().Add(new TenantMember
        {
            UserId = _actorId,
            TenantId = _tenantId,
            Role = TenantRole.Member.ToString(),
            IsActive = true,
        });
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task RegistrationAndQueries_AreStrictlyScopedToTheSelectedTenant()
    {
        var localSession = Session(_tenantId, 1);
        var foreignSession = Session(Guid.NewGuid(), 1);
        _context.Set<TestingSession>().AddRange(localSession, foreignSession);
        _context.Set<SessionRegistration>().Add(new SessionRegistration
        {
            TenantId = foreignSession.TenantId,
            SessionId = foreignSession.Id,
            UserId = Guid.NewGuid(),
        });
        await _context.SaveChangesAsync();
        var service = new TestingParticipantOperationsService(_context, _actors.Object);

        var created = await service.RegisterForSessionAsync(localSession.Id, _actorId, RegistrationType.Tester);
        var foreignRows = await service.GetSessionRegistrationsAsync(foreignSession.Id);
        var crossTenantRegistration = () => service.RegisterForSessionAsync(
            foreignSession.Id, _actorId, RegistrationType.Tester);

        created.TenantId.Should().Be(_tenantId);
        foreignRows.Should().BeEmpty();
        await crossTenantRegistration.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Unregister_PromotesTheOldestWaitlistedUserWithinTheSameTenant()
    {
        var session = Session(_tenantId, 1);
        var waitlistedUserId = Guid.NewGuid();
        _context.Set<TestingSession>().Add(session);
        await _context.SaveChangesAsync();
        var service = new TestingParticipantOperationsService(_context, _actors.Object);
        await service.RegisterForSessionAsync(session.Id, _actorId, RegistrationType.Tester);
        _context.Set<SessionWaitlist>().Add(new SessionWaitlist
        {
            TenantId = _tenantId,
            SessionId = session.Id,
            UserId = waitlistedUserId,
            RegistrationType = RegistrationType.Tester,
            Position = 1,
        });
        await _context.SaveChangesAsync();

        (await service.UnregisterFromSessionAsync(session.Id, _actorId)).Should().BeTrue();

        var promoted = await _context.Set<SessionRegistration>()
            .SingleAsync(registration => registration.SessionId == session.Id);
        promoted.UserId.Should().Be(waitlistedUserId);
        promoted.TenantId.Should().Be(_tenantId);
        (await _context.Set<SessionWaitlist>().Where(entry => entry.SessionId == session.Id).ToListAsync())
            .Should().BeEmpty();
    }

    [Fact]
    public async Task ParticipantCrud_ValidatesRequestAndMembershipAndIsIdempotent()
    {
        var service = new TestingParticipantOperationsService(_context, _actors.Object);
        await FluentActions.Awaiting(() => service.AddParticipantAsync(Guid.NewGuid(), _actorId))
            .Should().ThrowAsync<KeyNotFoundException>();

        var request = Request(_tenantId, _actorId);
        _context.Set<TestingRequest>().Add(request);
        await _context.SaveChangesAsync();
        await FluentActions.Awaiting(() => service.AddParticipantAsync(request.Id, Guid.NewGuid()))
            .Should().ThrowAsync<InvalidOperationException>();

        var created = await service.AddParticipantAsync(request.Id, _actorId);
        (await service.AddParticipantAsync(request.Id, _actorId)).Should().BeSameAs(created);
        (await service.GetTestingRequestParticipantsAsync(request.Id)).Should().ContainSingle();
        (await service.IsUserParticipantAsync(request.Id, _actorId)).Should().BeTrue();
        (await service.RemoveParticipantAsync(request.Id, _actorId)).Should().BeTrue();
        (await service.RemoveParticipantAsync(request.Id, _actorId)).Should().BeFalse();
        (await service.IsUserParticipantAsync(request.Id, _actorId)).Should().BeFalse();
    }

    [Fact]
    public async Task Register_RejectsClosedAndFullSessionsAndSupportsProjectMembers()
    {
        var service = new TestingParticipantOperationsService(_context, _actors.Object);
        var closed = Session(_tenantId, 1);
        closed.Status = SessionStatus.Active;
        var full = Session(_tenantId, 1);
        full.RegisteredTesterCount = 1;
        var projectSession = Session(_tenantId, 1);
        _context.Set<TestingSession>().AddRange(closed, full, projectSession);
        _context.Set<SessionWaitlist>().Add(new SessionWaitlist
        {
            TenantId = _tenantId,
            SessionId = projectSession.Id,
            UserId = _actorId,
            RegistrationType = RegistrationType.ProjectMember,
            Position = 1
        });
        await _context.SaveChangesAsync();

        await FluentActions.Awaiting(() => service.RegisterForSessionAsync(closed.Id, _actorId, RegistrationType.Tester))
            .Should().ThrowAsync<InvalidOperationException>();
        await FluentActions.Awaiting(() => service.RegisterForSessionAsync(full.Id, _actorId, RegistrationType.Tester))
            .Should().ThrowAsync<InvalidOperationException>();

        var registration = await service.RegisterForSessionAsync(
            projectSession.Id,
            _actorId,
            RegistrationType.ProjectMember,
            "Team Aurora");
        registration.RegistrationNotes.Should().Be("Team Aurora");
        projectSession.RegisteredProjectMemberCount.Should().Be(1);
        (await service.RegisterForSessionAsync(projectSession.Id, _actorId, RegistrationType.ProjectMember))
            .Should().BeSameAs(registration);
        (await service.GetSessionRegistrationsAsync(projectSession.Id)).Should().ContainSingle();
        _context.Set<SessionWaitlist>().Should().BeEmpty();
        (await service.UnregisterFromSessionAsync(projectSession.Id, _actorId)).Should().BeTrue();
        projectSession.RegisteredProjectMemberCount.Should().Be(0);
        (await service.UnregisterFromSessionAsync(projectSession.Id, _actorId)).Should().BeFalse();
    }

    [Fact]
    public async Task Waitlist_ValidatesSessionAndRegistrationAndMaintainsPositions()
    {
        var service = new TestingParticipantOperationsService(_context, _actors.Object);
        await FluentActions.Awaiting(() => service.AddToWaitlistAsync(Guid.NewGuid(), _actorId, RegistrationType.Tester))
            .Should().ThrowAsync<KeyNotFoundException>();

        var session = Session(_tenantId, 1);
        _context.Set<TestingSession>().Add(session);
        _context.Set<SessionRegistration>().Add(new SessionRegistration
        {
            TenantId = _tenantId,
            SessionId = session.Id,
            UserId = _actorId,
            RegistrationType = RegistrationType.Tester
        });
        await _context.SaveChangesAsync();
        await FluentActions.Awaiting(() => service.AddToWaitlistAsync(session.Id, _actorId, RegistrationType.Tester))
            .Should().ThrowAsync<InvalidOperationException>();

        _context.Set<SessionRegistration>().RemoveRange(_context.Set<SessionRegistration>());
        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"waitlist-{Guid.NewGuid():N}@example.com",
            Name = "Earlier waitlisted tester",
            IsActive = true
        };
        _context.Set<User>().Add(otherUser);
        _context.Set<SessionWaitlist>().Add(new SessionWaitlist
        {
            TenantId = _tenantId,
            SessionId = session.Id,
            Session = session,
            UserId = otherUser.Id,
            User = otherUser,
            RegistrationType = RegistrationType.Tester,
            Position = 3
        });
        await _context.SaveChangesAsync();

        var created = await service.AddToWaitlistAsync(session.Id, _actorId, RegistrationType.Tester, "Morning only");
        created.Position.Should().Be(4);
        (await service.AddToWaitlistAsync(session.Id, _actorId, RegistrationType.Tester)).Should().BeSameAs(created);
        (await service.GetSessionWaitlistAsync(session.Id)).Select(entry => entry.Position).Should().Equal(3, 4);
        (await service.RemoveFromWaitlistAsync(session.Id, _actorId)).Should().BeTrue();
        (await service.RemoveFromWaitlistAsync(session.Id, _actorId)).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveWaitlist_ClosesPositionGap()
    {
        var service = new TestingParticipantOperationsService(_context, _actors.Object);
        var session = Session(_tenantId, 2);
        var later = new SessionWaitlist
        {
            TenantId = _tenantId,
            SessionId = session.Id,
            UserId = Guid.NewGuid(),
            RegistrationType = RegistrationType.Tester,
            Position = 2
        };
        _context.Set<TestingSession>().Add(session);
        _context.Set<SessionWaitlist>().AddRange(new SessionWaitlist
        {
            TenantId = _tenantId,
            SessionId = session.Id,
            UserId = _actorId,
            RegistrationType = RegistrationType.Tester,
            Position = 1
        }, later);
        await _context.SaveChangesAsync();

        (await service.RemoveFromWaitlistAsync(session.Id, _actorId)).Should().BeTrue();
        later.Position.Should().Be(1);
    }

    [Fact]
    public async Task ParticipationRequiresOwnIdentityActiveMembershipAndSelectedTenant()
    {
        var session = Session(_tenantId, 2);
        _context.Set<TestingSession>().Add(session);
        await _context.SaveChangesAsync();
        var service = new TestingParticipantOperationsService(_context, _actors.Object);

        await FluentActions.Awaiting(() => service.RegisterForSessionAsync(session.Id, Guid.NewGuid(), RegistrationType.Tester))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _context.Set<TenantMember>().Single().IsActive = false;
        await _context.SaveChangesAsync();
        await FluentActions.Awaiting(() => service.RegisterForSessionAsync(session.Id, _actorId, RegistrationType.Tester))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        var noTenant = new Mock<IActorContextAccessor>();
        noTenant.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = _actorId.ToString(),
            TenantId = null,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        });
        var unscoped = new TestingParticipantOperationsService(_context, noTenant.Object);
        await FluentActions.Awaiting(() => unscoped.GetSessionWaitlistAsync(session.Id))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ActivityAndAttendanceReports_AggregateTenantEvidence()
    {
        var request = Request(_tenantId, _actorId);
        var session = Session(_tenantId, 2);
        session.TestingRequestId = request.Id;
        session.ManagerUserId = _actorId;
        session.SessionDate = new DateTime(2026, 2, 1);
        var registration = new SessionRegistration
        {
            TenantId = _tenantId,
            SessionId = session.Id,
            Session = session,
            UserId = _actorId,
            RegistrationType = RegistrationType.Tester,
            RegistrationNotes = "Team Aurora",
            AttendanceStatus = AttendanceStatus.Present
        };
        _context.Set<TestingRequest>().Add(request);
        _context.Set<TestingSession>().Add(session);
        _context.Set<TestingParticipant>().Add(new TestingParticipant
        {
            TenantId = _tenantId,
            TestingRequestId = request.Id,
            TestingRequest = request,
            UserId = _actorId,
            Status = ParticipationStatus.Completed
        });
        _context.Set<SessionRegistration>().Add(registration);
        _context.Set<TestingFeedback>().Add(new TestingFeedback
        {
            TenantId = _tenantId,
            TestingRequestId = request.Id,
            UserId = _actorId
        });
        await _context.SaveChangesAsync();
        var service = new TestingParticipantOperationsService(_context, _actors.Object);

        (await service.GetUserTestingActivityAsync(_actorId)).Should().BeEquivalentTo(new
        {
            ParticipationCount = 1,
            SessionRegistrationCount = 1,
            FeedbackCount = 1,
            ManagedSessionCount = 1,
            CreatedRequestCount = 1
        });
        var rows = (IReadOnlyCollection<StudentAttendanceReportRow>)await service.GetStudentAttendanceReportAsync();
        var row = rows.Should().ContainSingle().Subject;
        row.Name.Should().Be("Tenant tester");
        row.Team.Should().Be("Team Aurora");
        row.Block1Sessions.Should().Be(1);
        row.TotalSessions.Should().Be(1);
        row.GamesTested.Should().Be(1);
        row.Status.Should().Be("onTrack");
    }

    [Fact]
    public async Task EmptyAttendanceReport_ReturnsNoRows()
    {
        var service = new TestingParticipantOperationsService(_context, _actors.Object);
        ((IReadOnlyCollection<StudentAttendanceReportRow>)await service.GetStudentAttendanceReportAsync())
            .Should().BeEmpty();
    }

    [Fact]
    public void AttendanceRow_CoversRiskStatesAttendanceSignalsTeamsAndCalendarBlocks()
    {
        var userId = Guid.NewGuid();
        var request = Request(_tenantId, userId);
        var april = Registration(userId, request.Id, 4, AttendanceStatus.Completed);
        var july = Registration(userId, request.Id, 7, AttendanceStatus.Registered);
        july.Status = RegistrationStatus.Attended;
        var october = Registration(userId, request.Id, 10, AttendanceStatus.Registered);
        october.CheckedInAt = SystemClock.UtcNow;
        var noShowByAttendance = Registration(userId, request.Id, 1, AttendanceStatus.NoShow);
        var noShowByStatus = Registration(userId, request.Id, 1, AttendanceStatus.Registered);
        noShowByStatus.Status = RegistrationStatus.NoShow;

        var onTrack = BuildAttendanceRow(
            userId,
            _context.Set<User>().Single(),
            [april, july],
            [new TestingParticipant
            {
                TestingRequestId = request.Id,
                TestingRequest = request,
                Status = ParticipationStatus.Registered,
                CompletedAt = SystemClock.UtcNow
            }],
            []);
        onTrack.Block2Sessions.Should().Be(1);
        onTrack.Block3Sessions.Should().Be(1);
        onTrack.Status.Should().Be("onTrack");
        onTrack.Team.Should().Be("Arena prototype");

        var monitor = BuildAttendanceRow(userId, null, [october], [], []);
        monitor.Name.Should().Be("Unknown user");
        monitor.Email.Should().BeEmpty();
        monitor.Block4Sessions.Should().Be(1);
        monitor.Status.Should().Be("monitor");
        monitor.Team.Should().BeNull();

        BuildAttendanceRow(userId, null, [noShowByAttendance], [], []).Status.Should().Be("atRisk");
        BuildAttendanceRow(userId, null, [noShowByStatus], [], []).Status.Should().Be("atRisk");
        BuildAttendanceRow(userId, null, [], [new TestingParticipant
        {
            TestingRequestId = request.Id,
            TestingRequest = request,
            Status = ParticipationStatus.Withdrawn
        }], []).Status.Should().Be("atRisk");
    }

    private static StudentAttendanceReportRow BuildAttendanceRow(
        Guid userId,
        User? user,
        IEnumerable<SessionRegistration> registrations,
        IEnumerable<TestingParticipant> participants,
        IEnumerable<TestingFeedback> feedback)
    {
        var method = typeof(TestingParticipantOperationsService)
            .GetMethod("BuildAttendanceRow", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (StudentAttendanceReportRow)method.Invoke(null, [userId, user, registrations, participants, feedback])!;
    }

    private static SessionRegistration Registration(
        Guid userId,
        Guid requestId,
        int month,
        AttendanceStatus attendanceStatus) => new()
    {
        UserId = userId,
        Session = new TestingSession
        {
            TestingRequestId = requestId,
            SessionDate = new DateTime(2026, month, 1)
        },
        AttendanceStatus = attendanceStatus
    };

    private static TestingSession Session(Guid tenantId, int maxTesters) => new()
    {
        TenantId = tenantId,
        TestingRequestId = Guid.NewGuid(),
        LocationId = Guid.NewGuid(),
        SessionName = "Tenant-scoped session",
        SessionDate = DateTime.UtcNow.AddDays(1),
        StartTime = DateTime.UtcNow.AddDays(1),
        EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
        MaxTesters = maxTesters,
        Status = SessionStatus.Scheduled,
        ManagerId = Guid.NewGuid(),
        ManagerUserId = Guid.NewGuid(),
        CreatedById = Guid.NewGuid(),
    };

    private static TestingRequest Request(Guid tenantId, Guid creatorId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Title = "Arena prototype",
        CreatedById = creatorId,
        StartDate = DateTime.UtcNow,
        EndDate = DateTime.UtcNow.AddDays(7),
        Status = TestingRequestStatus.Open
    };

    private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options), IApplicationDbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<TenantMember> TenantMembers => Set<TenantMember>();
        public DbSet<TestingRequest> TestingRequests => Set<TestingRequest>();
        public DbSet<TestingParticipant> TestingParticipants => Set<TestingParticipant>();
        public DbSet<TestingSession> TestingSessions => Set<TestingSession>();
        public DbSet<SessionRegistration> SessionRegistrations => Set<SessionRegistration>();
        public DbSet<SessionWaitlist> SessionWaitlists => Set<SessionWaitlist>();
        public DbSet<TestingFeedback> TestingFeedback => Set<TestingFeedback>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
