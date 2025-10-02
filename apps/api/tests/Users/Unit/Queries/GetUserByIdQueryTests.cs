using FluentAssertions;
using GameGuild.Modules.Users;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Queries;

/// <summary>
/// Unit tests for GetUserByIdQuery
/// </summary>
public class GetUserByIdQueryTests
{
    [Fact]
    public void Query_Should_Have_Required_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserByIdQuery { UserId = userId };

        // Assert
        query.UserId.Should().Be(userId);
    }

    [Fact]
    public void Query_Should_Default_IncludeDeleted_To_False()
    {
        // Arrange & Act
        var query = new GetUserByIdQuery { UserId = Guid.NewGuid() };

        // Assert
        query.IncludeDeleted.Should().BeFalse();
    }
}
