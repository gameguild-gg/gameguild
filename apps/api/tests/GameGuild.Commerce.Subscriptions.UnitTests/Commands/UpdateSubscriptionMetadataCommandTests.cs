using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class UpdateSubscriptionMetadataCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var metadata = "{\"key\": \"value\", \"number\": 42}";
        var command = new UpdateSubscriptionMetadataCommand(subscriptionId, metadata);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var metadata = "{\"key\": \"value\"}";
        var command1 = new UpdateSubscriptionMetadataCommand(subscriptionId, metadata);
        var command2 = new UpdateSubscriptionMetadataCommand(subscriptionId, metadata);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentMetadata()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new UpdateSubscriptionMetadataCommand(subscriptionId, "{\"v\": 1}");
        var command2 = new UpdateSubscriptionMetadataCommand(subscriptionId, "{\"v\": 2}");

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
