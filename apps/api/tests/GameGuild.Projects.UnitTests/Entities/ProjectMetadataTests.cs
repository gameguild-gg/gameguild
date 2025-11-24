namespace GameGuild.Projects.UnitTests.Entities;

public class ProjectMetadataTests
{
    [Fact]
    public void ProjectMetadata_Creation_Should_Set_Default_Values()
    {
        // Arrange & Act
        var metadata = new ProjectMetadata();

        // Assert
        metadata.Key.Should().BeEmpty();
        metadata.Value.Should().BeEmpty();
        metadata.Id.Should().NotBeEmpty();
        metadata.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        metadata.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("engine", "Unity")]
    [InlineData("platform", "Windows,Mac,Linux")]
    [InlineData("genre", "Action,Adventure")]
    [InlineData("license", "MIT")]
    public void ProjectMetadata_Should_Accept_Valid_Key_Value_Pairs(string key, string value)
    {
        // Arrange & Act
        var metadata = new ProjectMetadata
        {
            ProjectId = Guid.NewGuid(),
            Key = key,
            Value = value
        };

        // Assert
        metadata.Key.Should().Be(key);
        metadata.Value.Should().Be(value);
        metadata.ProjectId.Should().NotBeEmpty();
    }

    [Fact]
    public void ProjectMetadata_Should_Have_Required_Project_Relationship()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        
        // Act
        var metadata = new ProjectMetadata
        {
            ProjectId = projectId,
            Key = "test-key",
            Value = "test-value"
        };

        // Assert
        metadata.ProjectId.Should().Be(projectId);
    }

    [Fact]
    public void ProjectMetadata_Should_Support_Empty_Values()
    {
        // Arrange & Act
        var metadata = new ProjectMetadata
        {
            ProjectId = Guid.NewGuid(),
            Key = "optional-field",
            Value = ""
        };

        // Assert
        metadata.Key.Should().Be("optional-field");
        metadata.Value.Should().BeEmpty();
    }

    [Fact]
    public void ProjectMetadata_Should_Track_Audit_Information()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        // Act
        var metadata = new ProjectMetadata
        {
            ProjectId = Guid.NewGuid(),
            Key = "test-key",
            Value = "test-value",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        metadata.CreatedAt.Should().Be(now);
        metadata.UpdatedAt.Should().Be(now);
        metadata.Id.Should().NotBeEmpty();
    }
}