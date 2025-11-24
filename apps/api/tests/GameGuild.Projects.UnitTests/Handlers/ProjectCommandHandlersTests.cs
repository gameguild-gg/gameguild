using GameGuild.Projects.UnitTests.Infrastructure;

namespace GameGuild.Projects.UnitTests.Handlers;

public class ProjectHandlersIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TestDataBuilder _testDataBuilder;
    private readonly Guid _testUserId;

    public ProjectHandlersIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
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
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Retrieve the project
        var retrievedProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);

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
        _context.Projects.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        // Assert
        var projects = await _context.Projects.ToListAsync();
        projects.Should().HaveCount(2);
        projects[0].Slug.Should().NotBe(projects[1].Slug);
    }

    [Fact]
    public async Task Database_Should_Support_Project_Collaborators()
    {
        // Arrange
        var project = _testDataBuilder.CreateProject(createdById: _testUserId);
        _context.Projects.Add(project);

        var collaborator = new ProjectCollaborator
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _testUserId,
            Role = "Owner",
            Permissions = "All",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
        _context.Projects.Add(project);

        var stats = new ProjectStatistics
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ViewCount = 100,
            DownloadCount = 50,
            LikeCount = 25,
            FollowerCount = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Set<ProjectStatistics>().Add(stats);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var savedStats = await _context.Set<ProjectStatistics>()
            .FirstOrDefaultAsync(s => s.ProjectId == project.Id);
        
        savedStats.Should().NotBeNull();
        savedStats!.ViewCount.Should().Be(100);
        savedStats.DownloadCount.Should().Be(50);
        savedStats.LikeCount.Should().Be(25);
        savedStats.FollowerCount.Should().Be(10);
    }

    [Fact]
    public async Task Database_Should_Support_Project_Metadata()
    {
        // Arrange
        var project = _testDataBuilder.CreateProject(createdById: _testUserId);
        _context.Projects.Add(project);

        var metadata = new ProjectMetadata
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Key = "engine",
            Value = "Unity",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Set<ProjectMetadata>().Add(metadata);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var savedMetadata = await _context.Set<ProjectMetadata>()
            .FirstOrDefaultAsync(m => m.ProjectId == project.Id);
        
        savedMetadata.Should().NotBeNull();
        savedMetadata!.Key.Should().Be("engine");
        savedMetadata.Value.Should().Be("Unity");
    }
}