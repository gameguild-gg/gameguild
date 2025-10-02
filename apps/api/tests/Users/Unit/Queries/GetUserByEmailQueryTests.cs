using FluentAssertions;
using GameGuild.Modules.Users;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Queries;

/// <summary>
/// Unit tests for GetUserByEmailQuery
/// </summary>
public class GetUserByEmailQueryTests
{
    [Fact]
    public void Query_Should_Have_Required_Properties()
    {
        // Arrange
        var email = "test@test.com";

        // Act
        var query = new GetUserByEmailQuery { Email = email };

        // Assert
        query.Email.Should().Be(email);
    }

    [Fact]
    public void Query_Should_Default_IncludeDeleted_To_False()
    {
        // Arrange & Act
        var query = new GetUserByEmailQuery { Email = "test@test.com" };

        // Assert
        query.IncludeDeleted.Should().BeFalse();
    }
}
