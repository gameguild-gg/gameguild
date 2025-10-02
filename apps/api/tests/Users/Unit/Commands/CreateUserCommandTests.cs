using FluentAssertions;
using GameGuild.Modules.Users;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Commands;

/// <summary>
/// Unit tests for CreateUserCommand
/// </summary>
public class CreateUserCommandTests
{
    [Fact]
    public void Command_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var command = new CreateUserCommand
        {
            Email = "test@test.com",
            GivenName = "John",
            FamilyName = "Doe",
            IsActive = true
        };

        // Assert
        command.Email.Should().Be("test@test.com");
        command.GivenName.Should().Be("John");
        command.FamilyName.Should().Be("Doe");
        command.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Command_Should_Default_IsActive_To_True()
    {
        // Arrange & Act
        var command = new CreateUserCommand { Email = "test@test.com" };

        // Assert
        command.IsActive.Should().BeTrue();
    }
}
