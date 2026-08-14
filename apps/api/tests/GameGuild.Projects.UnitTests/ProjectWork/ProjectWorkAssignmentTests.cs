using GameGuild.API.Database;
using GameGuild.ProjectWork;
using GameGuild.Teams;

namespace GameGuild.Projects.UnitTests.ProjectWork;

public sealed class ProjectWorkAssignmentTests : IDisposable
{
    private readonly ApplicationDbContext _context = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task IsEligibleAssignee_Should_Require_Active_Allocation_And_Active_Team_Membership()
    {
        var tenantId = Guid.NewGuid();
        var project = new Project { TenantId = tenantId, Title = "Game", Slug = "game" };
        var team = Team.Create(tenantId, "Studio", "studio", Guid.NewGuid());
        var userId = Guid.NewGuid();
        var projectTeam = project.AddParticipatingTeam(team.Id, ProjectTeamRole.Contributor);
        project.AddAllocation(projectTeam.Id, userId, "Developer", 50, SystemClock.UtcNow.AddDays(-1), SystemClock.UtcNow.AddDays(1));
        _context.Set<Team>().Add(team);
        _context.Set<Project>().Add(project);
        await _context.SaveChangesAsync();

        (await ProjectWorkAssignmentPolicy.IsEligibleAsync(_context, project.Id, userId, SystemClock.UtcNow)).Should().BeFalse();

        _context.Set<TeamMember>().Add(new TeamMember
        {
            TenantId = tenantId,
            TeamId = team.Id,
            UserId = userId,
            Authority = TeamMemberAuthority.Member,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        (await ProjectWorkAssignmentPolicy.IsEligibleAsync(_context, project.Id, userId, SystemClock.UtcNow)).Should().BeTrue();
    }

    [Fact]
    public async Task IsEligibleAssignee_Should_Reject_Expired_Allocation()
    {
        var tenantId = Guid.NewGuid();
        var project = new Project { TenantId = tenantId, Title = "Game", Slug = "game" };
        var userId = Guid.NewGuid();
        var team = Team.Create(tenantId, "Studio", "studio", userId);
        var projectTeam = project.AddParticipatingTeam(team.Id, ProjectTeamRole.Contributor);
        project.AddAllocation(projectTeam.Id, userId, "Developer", 50, SystemClock.UtcNow.AddDays(-2), SystemClock.UtcNow.AddDays(-1));
        _context.Set<Team>().Add(team);
        _context.Set<Project>().Add(project);
        await _context.SaveChangesAsync();

        (await ProjectWorkAssignmentPolicy.IsEligibleAsync(_context, project.Id, userId, SystemClock.UtcNow)).Should().BeFalse();
    }
}
