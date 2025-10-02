using FluentAssertions;
using GameGuild.Modules.UserProfiles;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Commands;

/// <summary>
/// Unit tests for DeleteUserProfileCommand
/// </summary>
public class DeleteUserProfileCommandTests
{
    [Fact]
    public void Command_Should_Have_Required_Properties()
    {
        // Arrange
        var userProfileId = Guid.NewGuid();

        // Act
        var command = new DeleteUserProfileCommand
        {
            UserProfileId = userProfileId,
            SoftDelete = false
        };

        // Assert
        command.UserProfileId.Should().Be(userProfileId);
        command.SoftDelete.Should().BeFalse();
    }

    [Fact]
    public void Command_Should_Default_SoftDelete_To_True()
    {
        // Arrange & Act
        var command = new DeleteUserProfileCommand
        {
            UserProfileId = Guid.NewGuid()
        };

        // Assert
        command.SoftDelete.Should().BeTrue();
    }
}
