using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Models;

public class UserMetadataDtoTests
{
    [Fact]
    public void UserMetadataDto_ShouldInstantiateWithAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var customFields = JsonMap(new Dictionary<string, object?>
        {
            { "field1", "value1" },
            { "field2", 123 }
        });
        var tags = new List<string> { "tag1", "tag2" };
        var externalRefs = new Dictionary<string, string>
        {
            { "system1", "ref1" },
            { "system2", "ref2" }
        };
        var createdAt = DateTimeOffset.UtcNow;
        var updatedAt = DateTimeOffset.UtcNow.AddHours(1);
        var version = new byte[] { 1, 2, 3, 4 };

        // Act
        var dto = new UserMetadataDto(
            id,
            userId,
            customFields,
            tags,
            externalRefs,
            createdAt,
            updatedAt,
            version
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.UserId.Should().Be(userId);
        dto.CustomFields.Should().BeEquivalentTo(customFields);
        dto.Tags.Should().BeEquivalentTo(tags);
        dto.ExternalReferences.Should().BeEquivalentTo(externalRefs);
        dto.CreatedAt.Should().Be(createdAt);
        dto.UpdatedAt.Should().Be(updatedAt);
        dto.Version.Should().BeEquivalentTo(version);
    }
}
