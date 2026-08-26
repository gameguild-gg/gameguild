using GameGuild.API.Database;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Teams;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.Projects.UnitTests.Teams;

public sealed class TeamAuthorizationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();

    public TeamAuthorizationServiceTests()
    {
        _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        SetActor("Member", _tenantId);
    }

    public void Dispose() => _context.Dispose();

    [Theory]
    [InlineData(TeamMemberAuthority.Viewer, TeamMemberAuthority.Viewer, true)]
    [InlineData(TeamMemberAuthority.Viewer, TeamMemberAuthority.Manager, false)]
    [InlineData(TeamMemberAuthority.Manager, TeamMemberAuthority.Manager, true)]
    [InlineData(TeamMemberAuthority.Owner, TeamMemberAuthority.Viewer, true)]
    public async Task HasAuthority_Should_Use_Typed_Active_Membership(
        TeamMemberAuthority actual,
        TeamMemberAuthority required,
        bool expected)
    {
        AddIdentity(_tenantId);
        var team = Team.Create(_tenantId, "Studio", "studio", Guid.NewGuid());
        team.AddMember(_actorId, actual);
        _context.Set<Team>().Add(team);
        await _context.SaveChangesAsync();

        var allowed = await CreateService().HasAuthorityAsync(team.Id, required);

        allowed.Should().Be(expected);
    }

    [Fact]
    public async Task Owner_Should_Satisfy_Viewer_Authority_With_String_Backed_Relational_Storage()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        await connection.OpenAsync();
        await using var context = new TeamAuthorizationTestDbContext(
            new DbContextOptionsBuilder<TeamAuthorizationTestDbContext>()
                .UseSqlite(connection)
                .Options);
        await context.Database.EnsureCreatedAsync();
        context.Set<User>().Add(new User { Id = _actorId, IsActive = true });
        context.Set<TenantMember>().Add(new TenantMember
        {
            UserId = _actorId,
            TenantId = _tenantId,
            IsActive = true
        });
        var team = Team.Create(_tenantId, "Studio", "studio", _actorId);
        context.Set<Team>().Add(team);
        await context.SaveChangesAsync();

        var allowed = await new TeamAuthorizationService(context, _actorAccessor.Object)
            .HasAuthorityAsync(team.Id, TeamMemberAuthority.Viewer);

        allowed.Should().BeTrue("an Owner must inherit Viewer access even when authority is stored as text");
    }

    [Fact]
    public async Task HasAuthority_Should_Reject_A_Membership_From_Another_Tenant()
    {
        AddIdentity(_tenantId);
        var team = Team.Create(Guid.NewGuid(), "Other", "other", _actorId);
        _context.Set<Team>().Add(team);
        await _context.SaveChangesAsync();

        var allowed = await CreateService().HasAuthorityAsync(team.Id, TeamMemberAuthority.Viewer);

        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task CanCreate_Should_Require_Active_Default_Tenant_Membership()
    {
        _context.Set<User>().Add(new User { Id = _actorId, IsActive = true });
        await _context.SaveChangesAsync();

        (await CreateService().CanCreateAsync()).Should().BeFalse();

        _context.Set<TenantMember>().Add(new TenantMember
        {
            UserId = _actorId,
            TenantId = _tenantId,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        (await CreateService().CanCreateAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task SystemAdmin_Should_Not_Bypass_Selected_Tenant_Membership()
    {
        SetActor("SystemAdmin", _tenantId);
        _context.Set<User>().Add(new User { Id = _actorId, IsActive = true });
        var team = Team.Create(_tenantId, "Studio", "studio", Guid.NewGuid());
        _context.Set<Team>().Add(team);
        await _context.SaveChangesAsync();

        (await CreateService().CanCreateAsync()).Should().BeFalse();
        (await CreateService().HasAuthorityAsync(team.Id, TeamMemberAuthority.Viewer)).Should().BeFalse();
        (await CreateService().ApplyMembershipAccess(_context.Set<Team>()).ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyPersonalAccess_Should_Not_Expose_Unrelated_Tenant_Teams_To_A_SystemAdmin()
    {
        SetActor("SystemAdmin", _tenantId);
        AddIdentity(_tenantId);
        var team = Team.Create(_tenantId, "Other studio", "other-studio", Guid.NewGuid());
        _context.Set<Team>().Add(team);
        await _context.SaveChangesAsync();

        var visible = await CreateService().ApplyPersonalAccess(_context.Set<Team>()).ToListAsync();

        visible.Should().NotContain(item => item.Id == team.Id);
    }

    [Fact]
    public void Restore_Should_Return_An_Archived_Team_To_The_Active_Workspace()
    {
        var team = Team.Create(_tenantId, "Studio", "studio", _actorId);
        team.Archive();

        team.Restore();

        team.Status.Should().Be(TeamStatus.Active);
        team.IsActive.Should().BeTrue();
    }

    private TeamAuthorizationService CreateService() => new(_context, _actorAccessor.Object);

    private void AddIdentity(Guid tenantId)
    {
        _context.Set<User>().Add(new User { Id = _actorId, IsActive = true });
        _context.Set<TenantMember>().Add(new TenantMember
        {
            UserId = _actorId,
            TenantId = tenantId,
            IsActive = true
        });
    }

    private void SetActor(string role, Guid tenantId) => _actorAccessor.SetupGet(accessor => accessor.ActorContext)
        .Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = _actorId.ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string> { role },
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        });

    private sealed class TeamAuthorizationTestDbContext(DbContextOptions<TeamAuthorizationTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasKey(user => user.Id);
            modelBuilder.Entity<TenantMember>().HasKey(member => member.Id);
            new TeamsModelConfiguration().Configure(modelBuilder);
        }

        Task<IDbContextTransaction> IApplicationDbContext.BeginTransactionAsync(
            CancellationToken cancellationToken) => Database.BeginTransactionAsync(cancellationToken);
    }
}
