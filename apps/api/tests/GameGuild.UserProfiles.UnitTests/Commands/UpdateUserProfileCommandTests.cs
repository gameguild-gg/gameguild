using FluentAssertions;
using GameGuild.UserProfiles;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Commands;

/// <summary>
/// Unit tests for UpdateUserProfileCommand
/// </summary>
public class UpdateUserProfileCommandTests
{
    [Fact]
    public void Command_Should_Have_Required_Properties()
    {
        // Arrange
        var userProfileId = Guid.NewGuid();
        var displayName = "Updated Name";
        var expectedVersion = 5;

        // Act
        var command = new UpdateUserProfileCommand
        {
            UserProfileId = userProfileId,
            DisplayName = displayName,
            ExpectedVersion = expectedVersion
        };

        // Assert
        command.UserProfileId.Should().Be(userProfileId);
        command.DisplayName.Should().Be(displayName);
        command.ExpectedVersion.Should().Be(expectedVersion);
    }
}
