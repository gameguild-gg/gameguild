using FluentAssertions;
using GameGuild.Modules.Users;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Commands;

/// <summary>
/// Unit tests for BulkActivateUsersCommand
/// </summary>
public class BulkActivateUsersCommandTests
{
    [Fact]
    public void Command_Should_Have_Required_Properties()
    {
        // Arrange
        var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var command = new BulkActivateUsersCommand { UserIds = userIds };

        // Assert
        command.UserIds.Should().BeEquivalentTo(userIds);
    }
}
