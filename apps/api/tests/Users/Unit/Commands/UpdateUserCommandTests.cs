using FluentAssertions;
using GameGuild.Modules.Users;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Commands;

/// <summary>
/// Unit tests for UpdateUserCommand
/// </summary>
public class UpdateUserCommandTests
{
    [Fact]
    public void Command_Should_Have_Required_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var command = new UpdateUserCommand
        {
            UserId = userId,
            GivenName = "Jane",
            FamilyName = "Smith"
        };

        // Assert
        command.UserId.Should().Be(userId);
        command.GivenName.Should().Be("Jane");
        command.FamilyName.Should().Be("Smith");
    }
}
