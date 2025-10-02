using FluentAssertions;
using GameGuild.Modules.Users;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Queries;

/// <summary>
/// Unit tests for GetUserByIdResultQuery
/// </summary>
public class GetUserByIdResultQueryTests
{
    [Fact]
    public void Query_Should_Have_Required_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserByIdResultQuery(userId);

        // Assert
        query.UserId.Should().Be(userId);
    }
}
