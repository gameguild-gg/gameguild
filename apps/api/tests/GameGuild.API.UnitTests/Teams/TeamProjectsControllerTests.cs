using FluentAssertions;
using GameGuild;
using GameGuild.API.Teams;
using GameGuild.Projects;
using GameGuild.Teams;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace GameGuild.API.UnitTests.Teams;

public sealed class TeamProjectsControllerTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TeamProjectsTestDbContext _context;
    private readonly Guid _tenantId = Guid.NewGuid();

    public TeamProjectsControllerTests()
    {
        _connection.Open();
        _context = new TeamProjectsTestDbContext(new DbContextOptionsBuilder<TeamProjectsTestDbContext>()
            .UseSqlite(_connection)
            .Options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task List_Should_Translate_Workspace_Access_And_Return_The_Teams_Project()
    {
        var teamId = Guid.NewGuid();
        var project = new Project
        {
            TenantId = _tenantId,
            Title = "Team project",
            Slug = "team-project",
            CreatedById = Guid.NewGuid()
        };
        var projectTeam = new ProjectTeam
        {
            TenantId = _tenantId,
            ProjectId = project.Id,
            Project = project,
            TeamId = teamId,
            Role = ProjectTeamRole.Owner,
            ParticipationMode = ProjectTeamParticipationMode.AllMembers,
            IsActive = true
        };
        project.Teams.Add(projectTeam);
        _context.Set<Project>().Add(project);
        await _context.SaveChangesAsync();

        var teamAuthorization = new Mock<ITeamAuthorizationService>();
        teamAuthorization
            .Setup(service => service.HasAuthorityAsync(teamId, TeamMemberAuthority.Viewer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var projectAuthorization = new Mock<IProjectAuthorizationService>();
        projectAuthorization
            .Setup(service => service.ApplyWorkspaceAccess(It.IsAny<IQueryable<Project>>(), false))
            .Returns((IQueryable<Project> query, bool _) => query);
        var controller = new TeamProjectsController(
            _context,
            teamAuthorization.Object,
            projectAuthorization.Object);

        var result = await controller.List(teamId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Which;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<TeamProjectSummary>>()
            .Which.Should().ContainSingle(row => row.Id == project.Id && row.TeamRole == ProjectTeamRole.Owner);
    }

    private sealed class TeamProjectsTestDbContext(DbContextOptions<TeamProjectsTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>(builder =>
            {
                builder.HasKey(project => project.Id);
                builder.Ignore(project => project.Category);
                builder.Ignore(project => project.ProjectMetadata);
                builder.Ignore(project => project.Versions);
                builder.Ignore(project => project.Collaborators);
                builder.Ignore(project => project.Releases);
                builder.Ignore(project => project.Allocations);
                builder.Ignore(project => project.TeamAgreements);
                builder.Ignore(project => project.Followers);
                builder.Ignore(project => project.Feedbacks);
                builder.Ignore(project => project.JamSubmissions);
                builder.Ignore(project => project.CreatedBy);
                builder.HasMany(project => project.Teams)
                    .WithOne(team => team.Project)
                    .HasForeignKey(team => team.ProjectId);
            });
            modelBuilder.Entity<ProjectTeam>(builder =>
            {
                builder.HasKey(team => team.Id);
                builder.Ignore(team => team.Team);
                builder.Ignore(team => team.Allocations);
            });
        }
    }
}
