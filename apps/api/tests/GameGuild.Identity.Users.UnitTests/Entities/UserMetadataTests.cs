using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Entities;

public class UserMetadataTests
{
    [Fact]
    public void Create_ShouldInitializeWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var metadata = UserMetadata.Create(userId);

        // Assert
        metadata.Should().NotBeNull();
        metadata.UserId.Should().Be(userId);
        metadata.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void SetCustomFields_ShouldStoreAndRetrieveCorrectly()
    {
        // Arrange
        var metadata = UserMetadata.Create(Guid.NewGuid());
        var fields = new Dictionary<string, object?>
        {
            ["department"] = "Engineering",
            ["level"] = 5,
            ["isManager"] = true
        };

        // Act
        metadata.SetCustomFields(fields);
        var retrieved = metadata.GetCustomFields();

        // Assert
        retrieved.Should().ContainKey("department");
        retrieved.Should().ContainKey("level");
        retrieved.Should().ContainKey("isManager");
    }

    [Fact]
    public void SetTags_ShouldStoreAndRetrieveCorrectly()
    {
        // Arrange
        var metadata = UserMetadata.Create(Guid.NewGuid());
        var tags = new List<string> { "premium", "verified", "early-adopter" };

        // Act
        metadata.SetTags(tags);
        var retrieved = metadata.GetTags();

        // Assert
        retrieved.Should().HaveCount(3);
        retrieved.Should().Contain("premium");
        retrieved.Should().Contain("verified");
        retrieved.Should().Contain("early-adopter");
    }

    [Fact]
    public void SetExternalReferences_ShouldStoreAndRetrieveCorrectly()
    {
        // Arrange
        var metadata = UserMetadata.Create(Guid.NewGuid());
        var references = new Dictionary<string, string>
        {
            ["slack"] = "U12345",
            ["jira"] = "user@example.com",
            ["github"] = "username"
        };

        // Act
        metadata.SetExternalReferences(references);
        var retrieved = metadata.GetExternalReferences();

        // Assert
        retrieved.Should().ContainKey("slack");
        retrieved.Should().ContainKey("jira");
        retrieved.Should().ContainKey("github");
    }

    [Fact]
    public void AddCustomField_ShouldAddToExistingFields()
    {
        // Arrange
        var metadata = UserMetadata.Create(Guid.NewGuid());
        metadata.SetCustomFields(new Dictionary<string, object?> { ["existing"] = "value" });

        // Act
        var fields = metadata.GetCustomFields();
        fields["new"] = "newValue";
        metadata.SetCustomFields(fields);
        var retrieved = metadata.GetCustomFields();

        // Assert
        retrieved.Should().ContainKey("existing");
        retrieved.Should().ContainKey("new");
    }

    [Fact]
    public void AddTag_ShouldAddToExistingTags()
    {
        // Arrange
        var metadata = UserMetadata.Create(Guid.NewGuid());
        metadata.SetTags(new List<string> { "existing" });

        // Act
        var tags = metadata.GetTags();
        tags.Add("newTag");
        metadata.SetTags(tags);
        var retrieved = metadata.GetTags();

        // Assert
        retrieved.Should().Contain("existing");
        retrieved.Should().Contain("newTag");
    }

    [Fact]
    public void RemoveTag_ShouldRemoveFromTags()
    {
        // Arrange
        var metadata = UserMetadata.Create(Guid.NewGuid());
        metadata.SetTags(new List<string> { "tag1", "tag2", "tag3" });

        // Act
        var tags = metadata.GetTags();
        tags.Remove("tag2");
        metadata.SetTags(tags);
        var retrieved = metadata.GetTags();

        // Assert
        retrieved.Should().HaveCount(2);
        retrieved.Should().Contain("tag1");
        retrieved.Should().Contain("tag3");
        retrieved.Should().NotContain("tag2");
    }

    [Fact]
    public void GetCustomFields_WhenSerializedValueIsNull_ShouldReturnEmptyDictionary()
    {
        var metadata = UserMetadata.Create(Guid.NewGuid());
        metadata.CustomFields = "null";

        var result = metadata.GetCustomFields();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTags_WhenSerializedValueIsNull_ShouldReturnEmptyList()
    {
        var metadata = UserMetadata.Create(Guid.NewGuid());
        metadata.Tags = "null";

        var result = metadata.GetTags();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetExternalReferences_WhenSerializedValueIsNull_ShouldReturnEmptyDictionary()
    {
        var metadata = UserMetadata.Create(Guid.NewGuid());
        metadata.ExternalReferences = "null";

        var result = metadata.GetExternalReferences();

        result.Should().BeEmpty();
    }
}
