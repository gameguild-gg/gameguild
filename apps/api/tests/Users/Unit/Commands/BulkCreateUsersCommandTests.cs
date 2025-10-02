using FluentAssertions;
using GameGuild.Modules.Users;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Commands;

/// <summary>
/// Unit tests for BulkCreateUsersCommand
/// </summary>
public class BulkCreateUsersCommandTests
{
    [Fact]
    public void Command_Should_Have_Required_Properties()
    {
        // Arrange
        var users = new List<CreateUserRequest>
        {
            new() { Email = "user1@test.com", GivenName = "User", FamilyName = "One" },
            new() { Email = "user2@test.com", GivenName = "User", FamilyName = "Two" }
        };

        // Act
        var command = new BulkCreateUsersCommand { Users = users };

        // Assert
        command.Users.Should().HaveCount(2);
    }
}
