using FluentAssertions;
using GameGuild.Users.Entities;
using Xunit;

namespace GameGuild.Users.UnitTests.Entities;

public class UserMetadataMethodsTests
{
    [Fact]
    public void GetCustomFields_WithValidJson_ShouldReturnDictionary()
    {
        // Arrange
        var metadata = new UserMetadata();
        metadata.CustomFields = "{\"key1\":\"value1\",\"key2\":42}";

        // Act
        var result = metadata.GetCustomFields();

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("key1");
        result["key1"]?.ToString().Should().Be("value1");
        result.Should().ContainKey("key2");
    }

    [Fact]
    public void GetCustomFields_WithInvalidJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var metadata = new UserMetadata();
        metadata.CustomFields = "invalid json";

        // Act
        var result = metadata.GetCustomFields();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void SetCustomFields_ShouldSerializeAndUpdateTimestamp()
    {
        // Arrange
        var metadata = new UserMetadata();
        var fields = new Dictionary<string, object?> { { "test", "value" }, { "number", 123 } };
        var originalUpdatedAt = metadata.UpdatedAt;

        // Act
        metadata.SetCustomFields(fields);

        // Assert
        metadata.CustomFields.Should().Contain("test");
        metadata.CustomFields.Should().Contain("value");
        metadata.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void GetTags_WithValidJson_ShouldReturnList()
    {
        // Arrange
        var metadata = new UserMetadata();
        metadata.Tags = "[\"tag1\",\"tag2\",\"tag3\"]";

        // Act
        var result = metadata.GetTags();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain("tag1");
        result.Should().Contain("tag2");
        result.Should().Contain("tag3");
    }

    [Fact]
    public void GetTags_WithInvalidJson_ShouldReturnEmptyList()
    {
        // Arrange
        var metadata = new UserMetadata();
        metadata.Tags = "invalid json";

        // Act
        var result = metadata.GetTags();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void SetTags_ShouldSerializeAndUpdateTimestamp()
    {
        // Arrange
        var metadata = new UserMetadata();
        var tags = new List<string> { "test", "development", "user" };
        var originalUpdatedAt = metadata.UpdatedAt;

        // Act
        metadata.SetTags(tags);

        // Assert
        metadata.Tags.Should().Contain("test");
        metadata.Tags.Should().Contain("development");
        metadata.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void GetExternalReferences_WithValidJson_ShouldReturnDictionary()
    {
        // Arrange
        var metadata = new UserMetadata();
        metadata.ExternalReferences = "{\"github\":\"user123\",\"slack\":\"U123456\"}";

        // Act
        var result = metadata.GetExternalReferences();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainKey("github");
        result["github"].Should().Be("user123");
        result.Should().ContainKey("slack");
        result["slack"].Should().Be("U123456");
    }

    [Fact]
    public void GetExternalReferences_WithInvalidJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var metadata = new UserMetadata();
        metadata.ExternalReferences = "invalid json";

        // Act
        var result = metadata.GetExternalReferences();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void SetExternalReferences_ShouldSerializeAndUpdateTimestamp()
    {
        // Arrange
        var metadata = new UserMetadata();
        var references = new Dictionary<string, string> { { "github", "user123" }, { "jira", "john.doe" } };
        var originalUpdatedAt = metadata.UpdatedAt;

        // Act
        metadata.SetExternalReferences(references);

        // Assert
        metadata.ExternalReferences.Should().Contain("github");
        metadata.ExternalReferences.Should().Contain("user123");
        metadata.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateNotes_ShouldUpdateNotesAndTimestamp()
    {
        // Arrange
        var metadata = new UserMetadata();
        var notes = "Important notes about this user";
        var originalUpdatedAt = metadata.UpdatedAt;

        // Act
        metadata.UpdateNotes(notes);

        // Assert
        metadata.Notes.Should().Be(notes);
        metadata.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateNotes_WithNull_ShouldSetNotesToNull()
    {
        // Arrange
        var metadata = new UserMetadata { Notes = "Existing notes" };
        var originalUpdatedAt = metadata.UpdatedAt;

        // Act
        metadata.UpdateNotes(null);

        // Assert
        metadata.Notes.Should().BeNull();
        metadata.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Create_WithMinimalParams_ShouldCreateValidMetadata()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var metadata = UserMetadata.Create(userId);

        // Assert
        metadata.Should().NotBeNull();
        metadata.UserId.Should().Be(userId);
        metadata.GetCustomFields().Should().BeEmpty();
        metadata.GetTags().Should().BeEmpty();
    }

    [Fact]
    public void Create_WithCustomFields_ShouldSetFieldsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var customFields = new Dictionary<string, object?> { { "role", "admin" }, { "level", 5 } };

        // Act
        var metadata = UserMetadata.Create(userId, customFields);

        // Assert
        metadata.Should().NotBeNull();
        metadata.UserId.Should().Be(userId);
        metadata.GetCustomFields().Should().ContainKey("role");
        metadata.GetCustomFields()["role"]?.ToString().Should().Be("admin");
        metadata.GetCustomFields().Should().ContainKey("level");
    }

    [Fact]
    public void Create_WithTags_ShouldSetTagsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tags = new List<string> { "premium", "beta-tester" };

        // Act
        var metadata = UserMetadata.Create(userId, tags: tags);

        // Assert
        metadata.Should().NotBeNull();
        metadata.UserId.Should().Be(userId);
        metadata.GetTags().Should().Contain("premium");
        metadata.GetTags().Should().Contain("beta-tester");
    }

    [Fact]
    public void Create_WithAllParams_ShouldSetAllFieldsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var customFields = new Dictionary<string, object?> { { "department", "engineering" } };
        var tags = new List<string> { "active", "verified" };

        // Act
        var metadata = UserMetadata.Create(userId, customFields, tags);

        // Assert
        metadata.Should().NotBeNull();
        metadata.UserId.Should().Be(userId);
        metadata.GetCustomFields().Should().ContainKey("department");
        metadata.GetTags().Should().Contain("active");
        metadata.GetTags().Should().Contain("verified");
    }
}