using GameGuild.Identity.Context.Actors;
using GameGuild.Projects.UnitTests.Infrastructure;
using GameGuild.Teams;
using System.Text.Json;

namespace GameGuild.Projects.UnitTests.Handlers;

public class ProjectHandlersIntegrationTests : IDisposable
{
    private readonly TestProjectsDbContext _context;
    private readonly TestDataBuilder _testDataBuilder;
    private readonly Guid _testUserId;

    public ProjectHandlersIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<TestProjectsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestProjectsDbContext(options);
        _testDataBuilder = new TestDataBuilder();
        _testUserId = Guid.NewGuid();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetProjectById_Should_Hide_A_Public_Draft_From_Anonymous_Users()
    {
        var project = _testDataBuilder.CreateProject(createdById: _testUserId, title: "Unpublished public project");
        project.Visibility = ContentVisibility.Public;
        project.Status = ContentStatus.Draft;
        _context.Set<Project>().Add(project);
        await _context.SaveChangesAsync();
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.SetupGet(accessor => accessor.ActorContext)
            .Returns(ActorContextBuilder.Create().Build());
        var handler = new ProjectQueryHandlers(
            _context,
            actorAccessor.Object,
            new ProjectAuthorizationService(_context, actorAccessor.Object),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectQueryHandlers>.Instance);

        var result = await handler.Handle(
            new GetProjectByIdQuery
            {
                ProjectId = project.Id,
                IncludeTeam = false,
                IncludeReleases = false,
                IncludeCollaborators = false
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetProjectById_IncludeTeam_LoadsProjectTeamsInsteadOfCollaborators()
    {
        var tenantId = Guid.NewGuid();
        var project = _testDataBuilder.CreateProject(createdById: _testUserId, title: "Team-owned project");
        project.TenantId = tenantId;
        project.Visibility = ContentVisibility.Public;
        project.Status = ContentStatus.Published;
        var team = Team.Create(tenantId, "Studio Team", "studio-team", _testUserId);
        var ownership = new ProjectTeam
        {
            TenantId = tenantId,
            ProjectId = project.Id,
            TeamId = team.Id,
            Role = ProjectTeamRole.Owner,
            ParticipationMode = ProjectTeamParticipationMode.AllMembers,
            IsActive = true,
        };
        _context.Set<Project>().Add(project);
        _context.Set<Team>().Add(team);
        _context.Set<ProjectTeam>().Add(ownership);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.SetupGet(accessor => accessor.ActorContext)
            .Returns(ActorContextBuilder.Create().Build());
        var handler = new ProjectQueryHandlers(
            _context,
            actorAccessor.Object,
            new ProjectAuthorizationService(_context, actorAccessor.Object),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectQueryHandlers>.Instance);

        var result = await handler.Handle(
            new GetProjectByIdQuery
            {
                ProjectId = project.Id,
                IncludeTeam = true,
                IncludeReleases = false,
                IncludeCollaborators = false,
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Teams.Should().ContainSingle(teamLink => teamLink.TeamId == team.Id);
    }

    [Fact]
    public async Task Database_Should_Store_And_Retrieve_Projects()
    {
        // Arrange
        var project = _testDataBuilder.CreateProject(createdById: _testUserId);
        project.Title = "Test Game Project";
        project.Description = "A test game project";
        project.Type = ProjectType.Game;

        // Act
        _context.Set<Project>().Add(project);
        await _context.SaveChangesAsync();

        // Retrieve the project
        var retrievedProject = await _context.Set<Project>().FirstOrDefaultAsync(p => p.Id == project.Id);

        // Assert
        retrievedProject.Should().NotBeNull();
        retrievedProject!.Title.Should().Be("Test Game Project");
        retrievedProject.Description.Should().Be("A test game project");
        retrievedProject.Type.Should().Be(ProjectType.Game);
        retrievedProject.CreatedById.Should().Be(_testUserId);
    }

    [Fact]
    public async Task Database_Should_Handle_Multiple_Projects_With_Unique_Slugs()
    {
        // Arrange
        var project1 = _testDataBuilder.CreateProject(createdById: _testUserId, title: "Test Project");
        var project2 = _testDataBuilder.CreateProject(createdById: _testUserId, title: "Test Project");
        
        project1.Slug = "test-project";
        project2.Slug = "test-project-2";

        // Act
        _context.Set<Project>().AddRange(project1, project2);
        await _context.SaveChangesAsync();

        // Assert
        var projects = await _context.Set<Project>().ToListAsync();
        projects.Should().HaveCount(2);
        projects[0].Slug.Should().NotBe(projects[1].Slug);
    }

    [Fact]
    public async Task GetAllProjects_CurrentTenantOnly_ShouldScopeSystemAdminToActiveTenant()
    {
        var actorTenantId = Guid.NewGuid();
        var actorProject = _testDataBuilder.CreateProject(createdById: _testUserId, title: "Current tenant project");
        actorProject.TenantId = actorTenantId;
        actorProject.Visibility = ContentVisibility.Public;
        var otherProject = _testDataBuilder.CreateProject(createdById: _testUserId, title: "Other tenant project");
        otherProject.TenantId = Guid.NewGuid();
        otherProject.Visibility = ContentVisibility.Public;
        _context.Set<Project>().AddRange(actorProject, otherProject);
        await _context.SaveChangesAsync();
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(
            ActorContextBuilder.ForUser(_testUserId)
                .WithTenantId(actorTenantId)
                .WithRole("SystemAdmin")
                .Build());
        var handler = new ProjectQueryHandlers(
            _context,
            actorAccessor.Object,
            new ProjectAuthorizationService(_context, actorAccessor.Object),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectQueryHandlers>.Instance);

        var result = await handler.Handle(
            new GetAllProjectsQuery { CurrentTenantOnly = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Id.Should().Be(actorProject.Id);
    }

    [Fact]
    public void ProjectSerialization_ShouldNotExposeCreatorAuthenticationData()
    {
        var project = _testDataBuilder.CreateProject(createdById: _testUserId);
        project.CreatedBy = new User
        {
            Id = _testUserId,
            Email = "private@example.com",
            Name = "Private creator",
            PasswordHash = "never-serialize-this-value"
        };

        var json = JsonSerializer.Serialize(project);

        json.Should().NotContain("\"CreatedBy\":");
        json.Should().NotContain("PasswordHash");
        json.Should().NotContain("never-serialize-this-value");
    }

    [Fact]
    public async Task Database_Should_Support_Project_Collaborators()
    {
        // Arrange
        var project = _testDataBuilder.CreateProject(createdById: _testUserId);
        _context.Set<Project>().Add(project);

        var collaborator = new ProjectCollaborator
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _testUserId,
            Role = "Owner",
            Permissions = "All",
            IsActive = true
        };
        _context.Set<ProjectCollaborator>().Add(collaborator);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var savedCollaborator = await _context.Set<ProjectCollaborator>()
            .FirstOrDefaultAsync(c => c.ProjectId == project.Id);
        
        savedCollaborator.Should().NotBeNull();
        savedCollaborator!.Role.Should().Be("Owner");
        savedCollaborator.IsActive.Should().BeTrue();
        savedCollaborator.UserId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task Database_Should_Support_Project_Statistics()
    {
        // Arrange
        var project = _testDataBuilder.CreateProject(createdById: _testUserId);
        _context.Set<Project>().Add(project);

        var stats = new ProjectMetadata
        {
            ProjectId = project.Id,
            ViewCount = 100,
            DownloadCount = 50,
            FollowerCount = 10
        };
        _context.Set<ProjectMetadata>().Add(stats);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var savedStats = await _context.Set<ProjectMetadata>()
            .FirstOrDefaultAsync(s => s.ProjectId == project.Id);
        
        savedStats.Should().NotBeNull();
        savedStats!.ViewCount.Should().Be(100);
        savedStats.DownloadCount.Should().Be(50);
        savedStats.FollowerCount.Should().Be(10);
    }

    [Fact]
    public async Task GetProjectStatistics_Should_Return_Downloads_From_Metadata_And_Releases()
    {
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor
            .SetupGet(accessor => accessor.ActorContext)
            .Returns(ActorContextBuilder.ForUser(_testUserId).WithRole("Admin").Build());
        var handler = new ProjectQueryHandlers(
            _context,
            actorAccessor.Object,
            new ProjectAuthorizationService(_context, actorAccessor.Object),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectQueryHandlers>.Instance);
        var project = _testDataBuilder.CreateProject(createdById: _testUserId);
        _context.Set<Project>().Add(project);
        _context.Set<ProjectMetadata>().Add(new ProjectMetadata
        {
            ProjectId = project.Id,
            DownloadCount = 7
        });
        _context.Set<ProjectRelease>().AddRange(
            new ProjectRelease { ProjectId = project.Id, Title = "v1", ReleaseVersion = "1.0.0", DownloadCount = 5 },
            new ProjectRelease { ProjectId = project.Id, Title = "v2", ReleaseVersion = "2.0.0", DownloadCount = 11 });
        await _context.SaveChangesAsync();

        var result = await handler.Handle(new GetProjectStatisticsQuery { ProjectId = project.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalDownloads.Should().Be(23);
    }

    [Fact]
    public async Task GetProjectStatistics_Should_Hide_An_Unauthorized_Private_Project()
    {
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor
            .SetupGet(accessor => accessor.ActorContext)
            .Returns(ActorContextBuilder.Create().Build());
        var handler = new ProjectQueryHandlers(
            _context,
            actorAccessor.Object,
            new ProjectAuthorizationService(_context, actorAccessor.Object),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectQueryHandlers>.Instance);
        var project = _testDataBuilder.CreateProject(createdById: _testUserId);
        project.Visibility = ContentVisibility.Private;
        _context.Set<Project>().Add(project);
        await _context.SaveChangesAsync();

        var result = await handler.Handle(
            new GetProjectStatisticsQuery { ProjectId = project.Id },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Project.NotFound");
    }

    [Fact]
    public async Task UpdateProject_Should_Hide_An_Unauthorized_Private_Project()
    {
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor
            .SetupGet(accessor => accessor.ActorContext)
            .Returns(ActorContextBuilder.Create().Build());
        var project = _testDataBuilder.CreateProject(createdById: _testUserId);
        project.Visibility = ContentVisibility.Private;
        _context.Set<Project>().Add(project);
        await _context.SaveChangesAsync();
        var handler = new ProjectCommandHandlers(
            _context,
            actorAccessor.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectCommandHandlers>.Instance);

        var result = await handler.Handle(
            new UpdateProjectCommand { ProjectId = project.Id, Title = "Should not leak" },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Project.NotFound");
    }

    [Fact]
    public async Task Database_Should_Support_Project_Metadata()
    {
        // Arrange
        var project = _testDataBuilder.CreateProject(createdById: _testUserId);
        _context.Set<Project>().Add(project);

        var metadata = new ProjectMetadata
        {
            ProjectId = project.Id,
            ViewCount = 12,
            DownloadCount = 4,
            FollowerCount = 2
        };
        _context.Set<ProjectMetadata>().Add(metadata);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var savedMetadata = await _context.Set<ProjectMetadata>()
            .FirstOrDefaultAsync(m => m.ProjectId == project.Id);
        
        savedMetadata.Should().NotBeNull();
        savedMetadata!.ViewCount.Should().Be(12);
        savedMetadata.DownloadCount.Should().Be(4);
        savedMetadata.FollowerCount.Should().Be(2);
    }
}
