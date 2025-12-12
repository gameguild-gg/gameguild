using FluentAssertions;
using GameGuild.UserProfiles;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Commands;

/// <summary>
/// Unit tests for BulkDeleteUserProfilesCommand
/// </summary>
public class BulkDeleteUserProfilesCommandTests
{
    [Fact]
    public void Command_Should_Have_Required_Properties()
    {
        // Arrange
        var userProfileIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var reason = "Test deletion";

        // Act
        var command = new BulkDeleteUserProfilesCommand
        {
            UserProfileIds = userProfileIds,
            SoftDelete = false,
            Reason = reason
        };

        // Assert
        command.UserProfileIds.Should().HaveCount(2);
        command.SoftDelete.Should().BeFalse();
        command.Reason.Should().Be(reason);
    }

    [Fact]
    public void Command_Should_Default_SoftDelete_To_True()
    {
        // Arrange & Act
        var command = new BulkDeleteUserProfilesCommand
        {
            UserProfileIds = new List<Guid> { Guid.NewGuid() }
        };

        // Assert
        command.SoftDelete.Should().BeTrue();
    }
}
