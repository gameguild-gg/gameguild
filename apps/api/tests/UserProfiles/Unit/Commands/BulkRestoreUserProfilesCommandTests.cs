using FluentAssertions;
using GameGuild.Modules.UserProfiles;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Commands;

/// <summary>
/// Unit tests for BulkRestoreUserProfilesCommand
/// </summary>
public class BulkRestoreUserProfilesCommandTests
{
    [Fact]
    public void Command_Should_Have_Required_Properties()
    {
        // Arrange
        var userProfileIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var reason = "Test restoration";

        // Act
        var command = new BulkRestoreUserProfilesCommand
        {
            UserProfileIds = userProfileIds,
            Reason = reason
        };

        // Assert
        command.UserProfileIds.Should().HaveCount(2);
        command.Reason.Should().Be(reason);
    }
}
