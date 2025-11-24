using FluentAssertions;
using GameGuild.UserProfiles;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Commands;

/// <summary>
/// Unit tests for RestoreUserProfileCommand
/// </summary>
public class RestoreUserProfileCommandTests
{
    [Fact]
    public void Command_Should_Have_Required_Properties()
    {
        // Arrange
        var userProfileId = Guid.NewGuid();

        // Act
        var command = new RestoreUserProfileCommand
        {
            UserProfileId = userProfileId
        };

        // Assert
        command.UserProfileId.Should().Be(userProfileId);
    }
}
