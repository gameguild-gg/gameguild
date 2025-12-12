using GameGuild.Projects.UnitTests.Infrastructure;

namespace GameGuild.Projects.UnitTests.Integration;

public class ProjectDatabaseIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TestDataBuilder _testDataBuilder;
    private readonly Guid _testUserId;

    public ProjectDatabaseIntegrationTests()
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
    public async Task Should_Filter_Projects_By_Type()
    {
        // Arrange
        var projects = new[]
        {
            _testDataBuilder.CreateProject(type: ProjectType.Game),
            _testDataBuilder.CreateProject(type: ProjectType.Tool),
            _testDataBuilder.CreateProject(type: ProjectType.Game)
        };

        _context.Projects.AddRange(projects);
        await _context.SaveChangesAsync();

        // Act
        var gameProjects = await _context.Projects
            .Where(p => p.Type == ProjectType.Game)
            .ToListAsync();

        // Assert
        gameProjects.Should().HaveCount(2);
        gameProjects.Should().OnlyContain(p => p.Type == ProjectType.Game);
    }

    [Fact]
    public async Task Should_Filter_Projects_By_Status()
    {
        // Arrange
        var projects = new[]
        {
            _testDataBuilder.CreateProject(),
            _testDataBuilder.CreateProject(),
            _testDataBuilder.CreateProject()
        };

        projects[0].Status = ContentStatus.Draft;
        projects[1].Status = ContentStatus.Published;
        projects[2].Status = ContentStatus.Published;

        _context.Projects.AddRange(projects);
        await _context.SaveChangesAsync();

        // Act
        var publishedProjects = await _context.Projects
            .Where(p => p.Status == ContentStatus.Published)
            .ToListAsync();

        // Assert
        publishedProjects.Should().HaveCount(2);
        publishedProjects.Should().OnlyContain(p => p.Status == ContentStatus.Published);
    }

    [Fact]
    public async Task Should_Exclude_Deleted_Projects_By_Default()
    {
        // Arrange
        var projects = new[]
        {
            _testDataBuilder.CreateProject(),
            _testDataBuilder.CreateProject(),
            _testDataBuilder.CreateProject()
        };

        // Mark one as deleted
        projects[2].DeletedAt = DateTime.UtcNow;

        _context.Projects.AddRange(projects);
        await _context.SaveChangesAsync();

        // Act
        var activeProjects = await _context.Projects
            .Where(p => p.DeletedAt == null)
            .ToListAsync();

        // Assert
        activeProjects.Should().HaveCount(2);
        activeProjects.Should().OnlyContain(p => p.DeletedAt == null);
    }

    [Fact]
    public async Task Should_Support_Project_Search()
    {
        // Arrange
        var projects = new[]
        {
            _testDataBuilder.CreateProject(title: "Awesome Game Project"),
            _testDataBuilder.CreateProject(title: "Cool Tool"),
            _testDataBuilder.CreateProject(title: "Another Game")
        };

        _context.Projects.AddRange(projects);
        await _context.SaveChangesAsync();

        // Act
        var searchResults = await _context.Projects
            .Where(p => p.Title.ToLower().Contains("game"))
            .ToListAsync();

        // Assert
        searchResults.Should().HaveCount(2);
        searchResults.Should().OnlyContain(p => p.Title.ToLower().Contains("game"));
    }

    [Fact]
    public async Task Should_Support_Pagination()
    {
        // Arrange
        var projects = Enumerable.Range(1, 10)
            .Select(i => _testDataBuilder.CreateProject(title: $"Project {i}"))
            .ToArray();

        _context.Projects.AddRange(projects);
        await _context.SaveChangesAsync();

        // Act
        var pagedResults = await _context.Projects
            .OrderBy(p => p.Title)
            .Skip(3)
            .Take(4)
            .ToListAsync();

        // Assert
        pagedResults.Should().HaveCount(4);
    }

    [Fact]
    public async Task Should_Filter_By_Creator()
    {
        // Arrange
        var creator1Id = Guid.NewGuid();
        var creator2Id = Guid.NewGuid();

        var projects = new[]
        {
            _testDataBuilder.CreateProject(createdById: creator1Id),
            _testDataBuilder.CreateProject(createdById: creator1Id),
            _testDataBuilder.CreateProject(createdById: creator2Id)
        };

        _context.Projects.AddRange(projects);
        await _context.SaveChangesAsync();

        // Act
        var creator1Projects = await _context.Projects
            .Where(p => p.CreatedById == creator1Id)
            .ToListAsync();

        // Assert
        creator1Projects.Should().HaveCount(2);
        creator1Projects.Should().OnlyContain(p => p.CreatedById == creator1Id);
    }

    [Fact]
    public async Task Should_Support_Project_Relationships()
    {
        // Arrange
        var project = _testDataBuilder.CreateProject(createdById: _testUserId);
        var category = _testDataBuilder.CreateProjectCategory("Games");
        
        project.CategoryId = category.Id;
        
        _context.ProjectCategories.Add(category);
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act
        var projectWithCategory = await _context.Projects
            .FirstOrDefaultAsync(p => p.CategoryId == category.Id);

        // Assert
        projectWithCategory.Should().NotBeNull();
        projectWithCategory!.CategoryId.Should().Be(category.Id);
    }
}