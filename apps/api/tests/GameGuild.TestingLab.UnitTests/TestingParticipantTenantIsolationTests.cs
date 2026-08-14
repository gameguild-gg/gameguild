using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
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
