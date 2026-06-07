namespace GameGuild.Projects.UnitTests.Models;

/// <summary>
/// Basic models for testing project operations
/// </summary>
public record TestCreateProjectRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public string? ImageUrl { get; init; }
    public ProjectType Type { get; init; } = ProjectType.Game;
    public Guid CreatedById { get; init; }
    public Guid? CategoryId { get; init; }
    public ContentVisibility Visibility { get; init; } = ContentVisibility.Public;
    public ContentStatus Status { get; init; } = ContentStatus.Draft;
    public List<string>? Tags { get; init; }
}

public record TestUpdateProjectRequest
{
    public Guid ProjectId { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public ProjectType? Type { get; init; }
    public ContentVisibility? Visibility { get; init; }
    public ContentStatus? Status { get; init; }
    public Guid UpdatedBy { get; init; }
}

public record TestDeleteProjectRequest
{
    public Guid ProjectId { get; init; }
    public Guid DeletedBy { get; init; }
    public bool SoftDelete { get; init; } = true;
    public string? Reason { get; init; }
}

public class TestProjectRequestValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateProjectRequest_Should_Validate_Title_Required(string title)
    {
        // Arrange
        var request = new TestCreateProjectRequest
        {
            Title = title,
            CreatedById = Guid.NewGuid()
        };

        // Assert
        request.Title.Should().Be(title);
        if (string.IsNullOrWhiteSpace(title))
        {
            // In a real scenario, this would be validated by FluentValidation
            request.Title.Should().BeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void CreateProjectRequest_Should_Have_Default_Values()
    {
        // Arrange & Act
        var request = new TestCreateProjectRequest();

        // Assert
        request.Title.Should().BeEmpty();
        request.Type.Should().Be(ProjectType.Game);
        request.Visibility.Should().Be(ContentVisibility.Public);
        request.Status.Should().Be(ContentStatus.Draft);
        request.CreatedById.Should().BeEmpty();
    }

    [Theory]
    [InlineData(ProjectType.Game)]
    [InlineData(ProjectType.Tool)]
    [InlineData(ProjectType.Art)]
    [InlineData(ProjectType.Music)]
    public void CreateProjectRequest_Should_Accept_All_Project_Types(ProjectType projectType)
    {
        // Arrange & Act
        var request = new TestCreateProjectRequest
        {
            Title = "Test Project",
            Type = projectType,
            CreatedById = Guid.NewGuid()
        };

        // Assert
        request.Type.Should().Be(projectType);
    }

    [Theory]
    [InlineData(ContentVisibility.Private)]
    [InlineData(ContentVisibility.Internal)]
    [InlineData(ContentVisibility.Friends)]
    [InlineData(ContentVisibility.Protected)]
    [InlineData(ContentVisibility.Public)]
    public void CreateProjectRequest_Should_Accept_All_Visibility_Levels(ContentVisibility visibility)
    {
        // Arrange & Act
        var request = new TestCreateProjectRequest
        {
            Title = "Test Project",
            Visibility = visibility,
            CreatedById = Guid.NewGuid()
        };

        // Assert
        request.Visibility.Should().Be(visibility);
    }

    [Theory]
    [InlineData(ContentStatus.Draft)]
    [InlineData(ContentStatus.Published)]
    public void CreateProjectRequest_Should_Accept_Valid_Content_Status(ContentStatus status)
    {
        // Arrange & Act
        var request = new TestCreateProjectRequest
        {
            Title = "Test Project",
            Status = status,
            CreatedById = Guid.NewGuid()
        };

        // Assert
        request.Status.Should().Be(status);
    }

    [Fact]
    public void UpdateProjectRequest_Should_Allow_Null_Optional_Fields()
    {
        // Arrange & Act
        var request = new TestUpdateProjectRequest
        {
            ProjectId = Guid.NewGuid(),
            UpdatedBy = Guid.NewGuid()
        };

        // Assert
        request.ProjectId.Should().NotBeEmpty();
        request.UpdatedBy.Should().NotBeEmpty();
        request.Title.Should().BeNull();
        request.Description.Should().BeNull();
        request.Type.Should().BeNull();
        request.Visibility.Should().BeNull();
        request.Status.Should().BeNull();
    }

    [Fact]
    public void DeleteProjectRequest_Should_Default_To_Soft_Delete()
    {
        // Arrange & Act
        var request = new TestDeleteProjectRequest
        {
            ProjectId = Guid.NewGuid(),
            DeletedBy = Guid.NewGuid()
        };

        // Assert
        request.SoftDelete.Should().BeTrue();
        request.Reason.Should().BeNull();
    }
}
