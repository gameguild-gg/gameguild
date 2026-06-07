using GameGuild.Projects.UnitTests.Infrastructure;

namespace GameGuild.Projects.UnitTests.Entities;

public class ProjectTests
{
    private readonly TestDataBuilder _testDataBuilder;

    public ProjectTests()
    {
        _testDataBuilder = new TestDataBuilder();
    }

    [Fact]
    public void Project_Creation_Should_Set_Default_Values()
    {
        // Arrange & Act
        var project = new Project();

        // Assert
        project.Title.Should().BeEmpty();
        project.Slug.Should().BeEmpty();
        project.Type.Should().Be(ProjectType.Game);
        project.DevelopmentStatus.Should().Be(DevelopmentStatus.Planning);
        project.Status.Should().Be(ContentStatus.Draft);
        project.Visibility.Should().Be(ContentVisibility.Private);
        project.Id.Should().NotBeEmpty();
        project.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        project.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("Test Project", "test-project")]
    [InlineData("My Game Project", "my-game-project")]
    [InlineData("Super Cool Tool", "super-cool-tool")]
    public void Project_Should_Allow_Valid_Title_And_Slug(string title, string expectedSlug)
    {
        // Arrange
        var project = _testDataBuilder.CreateProject(title: title);

        // Act
        project.Slug = expectedSlug;

        // Assert
        project.Title.Should().Be(title);
        project.Slug.Should().Be(expectedSlug);
    }

    [Fact]
    public void Project_Should_Have_Required_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var project = _testDataBuilder.CreateProject(createdById: userId);

        // Act & Assert
        project.Id.Should().NotBeEmpty();
        project.Title.Should().NotBeNullOrEmpty();
        project.Slug.Should().NotBeNullOrEmpty();
        project.CreatedById.Should().Be(userId);
        project.CreatedAt.Should().BeAfter(DateTime.MinValue);
        project.UpdatedAt.Should().BeAfter(DateTime.MinValue);
    }

    [Theory]
    [InlineData(ProjectType.Game)]
    [InlineData(ProjectType.Tool)]
    [InlineData(ProjectType.Art)]
    [InlineData(ProjectType.Music)]
    public void Project_Should_Support_All_Project_Types(ProjectType projectType)
    {
        // Arrange & Act
        var project = _testDataBuilder.CreateProject(type: projectType);

        // Assert
        project.Type.Should().Be(projectType);
    }

    [Theory]
    [InlineData(DevelopmentStatus.Planning)]
    [InlineData(DevelopmentStatus.InDevelopment)]
    [InlineData(DevelopmentStatus.Alpha)]
    [InlineData(DevelopmentStatus.Beta)]
    [InlineData(DevelopmentStatus.Released)]
    [InlineData(DevelopmentStatus.Completed)]
    [InlineData(DevelopmentStatus.OnHold)]
    [InlineData(DevelopmentStatus.Cancelled)]
    [InlineData(DevelopmentStatus.Archived)]
    public void Project_Should_Support_All_Development_Statuses(DevelopmentStatus status)
    {
        // Arrange
        var project = _testDataBuilder.CreateProject();

        // Act
        project.DevelopmentStatus = status;

        // Assert
        project.DevelopmentStatus.Should().Be(status);
    }

    [Theory]
    [InlineData(ContentStatus.Draft)]
    [InlineData(ContentStatus.Published)]
    [InlineData(ContentStatus.Archived)]
    public void Project_Should_Support_All_Content_Statuses(ContentStatus status)
    {
        // Arrange
        var project = _testDataBuilder.CreateProject();

        // Act
        project.Status = status;

        // Assert
        project.Status.Should().Be(status);
    }

    [Theory]
    [InlineData(ContentVisibility.Private)]
    [InlineData(ContentVisibility.Internal)]
    [InlineData(ContentVisibility.Friends)]
    [InlineData(ContentVisibility.Protected)]
    [InlineData(ContentVisibility.Public)]
    public void Project_Should_Support_All_Visibility_Levels(ContentVisibility visibility)
    {
        // Arrange
        var project = _testDataBuilder.CreateProject();

        // Act
        project.Visibility = visibility;

        // Assert
        project.Visibility.Should().Be(visibility);
    }

    [Fact]
    public void Project_Should_Track_Audit_Information()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        
        // Act
        var project = new Project
        {
            Title = "Test Project",
            Slug = "test-project",
            CreatedById = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        project.CreatedById.Should().Be(userId);
        project.CreatedAt.Should().Be(now);
        project.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Project_Should_Allow_Optional_Fields_To_Be_Null()
    {
        // Arrange & Act
        var project = new Project
        {
            Title = "Test Project",
            Slug = "test-project"
        };

        // Assert
        project.ShortDescription.Should().BeNull();
        project.Description.Should().BeNull();
        project.ImageUrl.Should().BeNull();
        project.RepositoryUrl.Should().BeNull();
        project.WebsiteUrl.Should().BeNull();
        project.DownloadUrl.Should().BeNull();
        project.CategoryId.Should().BeNull();
    }

    [Fact]
    public void Project_Should_Support_Metadata_Fields()
    {
        // Arrange
        var project = _testDataBuilder.CreateProject();
        var repositoryUrl = "https://github.com/user/repo";
        var websiteUrl = "https://example.com";
        var downloadUrl = "https://example.com/download";
        var imageUrl = "https://example.com/image.png";

        // Act
        project.RepositoryUrl = repositoryUrl;
        project.WebsiteUrl = websiteUrl;
        project.DownloadUrl = downloadUrl;
        project.ImageUrl = imageUrl;

        // Assert
        project.RepositoryUrl.Should().Be(repositoryUrl);
        project.WebsiteUrl.Should().Be(websiteUrl);
        project.DownloadUrl.Should().Be(downloadUrl);
        project.ImageUrl.Should().Be(imageUrl);
    }
}
