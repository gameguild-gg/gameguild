using FluentAssertions;
using GameGuild.Modules.Users;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Commands;

/// <summary>
/// Unit tests for DeactivateUserCommand
/// </summary>
public class DeactivateUserCommandTests
{
    [Fact]
    public void Command_Should_Have_Required_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var command = new DeactivateUserCommand { UserId = userId };

        // Assert
        command.UserId.Should().Be(userId);
    }
}
