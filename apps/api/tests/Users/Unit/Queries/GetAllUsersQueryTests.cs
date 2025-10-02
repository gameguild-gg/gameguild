using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Users;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Queries;

/// <summary>
/// Unit tests for GetAllUsersQuery
/// </summary>
public class GetAllUsersQueryTests
{
    [Fact]
    public void Query_Should_Implement_IRequest_And_ICachedRequest()
    {
        // Arrange & Act
        var query = new GetAllUsersQuery();

        // Assert
        query.Should().BeAssignableTo<IRequest<IEnumerable<User>>>();
        query.Should().BeAssignableTo<ICachedRequest>();
    }

    [Fact]
    public void Query_Should_Have_Correct_Cache_Key()
    {
        // Arrange
        var query = new GetAllUsersQuery { IncludeDeleted = true, IsActive = true, Skip = 10, Take = 20 };

        // Act
        var cacheKey = query.CacheKey;

        // Assert
        cacheKey.Should().Be("users:all:True:True:10:20");
    }

    [Fact]
    public void Query_Should_Have_Cache_Expiration()
    {
        // Arrange
        var query = new GetAllUsersQuery();

        // Act
        var cacheExpiration = query.CacheExpiration;
        var slidingExpiration = query.SlidingExpiration;

        // Assert
        cacheExpiration.Should().Be(TimeSpan.FromMinutes(10));
        slidingExpiration.Should().Be(TimeSpan.FromMinutes(2));
    }
}
