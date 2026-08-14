using GameGuild.API.Database;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Teams;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameGuild.Projects.UnitTests.Handlers;

public sealed class ProjectCreationOwnershipTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();

    public ProjectCreationOwnershipTests()
    {
        _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = _actorId.ToString(),
            TenantId = _tenantId,
            Roles = new HashSet<string> { "Member" },
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        });
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Create_Should_Create_Personal_Team_When_Owner_Team_Is_Not_Provided()
    {
        AddIdentity();
        await _context.SaveChangesAsync();

        var result = await Handler().Handle(new CreateProjectCommand { Title = "Solo game" }, default);

        result.IsSuccess.Should().BeTrue();
        var ownerTeam = await _context.Set<ProjectTeam>().SingleAsync(candidate => candidate.ProjectId == result.Value.Id);
        ownerTeam.Role.Should().Be(ProjectTeamRole.Owner);
        var team = await _context.Set<Team>().Include(candidate => candidate.Members).SingleAsync(candidate => candidate.Id == ownerTeam.TeamId);
        team.IsPersonal.Should().BeTrue();
        team.Members.Should().ContainSingle(member => member.UserId == _actorId && member.Authority == TeamMemberAuthority.Owner);
    }

    [Fact]
    public async Task Create_Should_Use_Provided_Team_Only_For_A_Manager()
    {
        AddIdentity();
        var team = Team.Create(_tenantId, "Studio", "studio", Guid.NewGuid());
        team.AddMember(_actorId, TeamMemberAuthority.Manager);
        _context.Set<Team>().Add(team);
        await _context.SaveChangesAsync();

        var result = await Handler().Handle(new CreateProjectCommand { Title = "Team game", OwnerTeamId = team.Id }, default);

        result.IsSuccess.Should().BeTrue();
        (await _context.Set<ProjectTeam>().SingleAsync(candidate => candidate.ProjectId == result.Value.Id)).TeamId.Should().Be(team.Id);
        (await _context.Set<Team>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Create_Should_Reject_Provided_Team_For_A_Viewer()
    {
        AddIdentity();
        var team = Team.Create(_tenantId, "Studio", "studio", Guid.NewGuid());
        team.AddMember(_actorId, TeamMemberAuthority.Viewer);
        _context.Set<Team>().Add(team);
        await _context.SaveChangesAsync();

        var result = await Handler().Handle(new CreateProjectCommand { Title = "Forbidden", OwnerTeamId = team.Id }, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Project.OwnerTeamForbidden");
    }

    private ProjectCommandHandlers Handler() => new(
        _context,
        _actorAccessor.Object,
        NullLogger<ProjectCommandHandlers>.Instance);

    private void AddIdentity()
    {
        _context.Set<User>().Add(new User { Id = _actorId, IsActive = true });
        _context.Set<TenantMember>().Add(new TenantMember
        {
            UserId = _actorId,
            TenantId = _tenantId,
            IsActive = true
        });
    }
}
