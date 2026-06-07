using GameGuild.Projects.UnitTests.Infrastructure;

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
