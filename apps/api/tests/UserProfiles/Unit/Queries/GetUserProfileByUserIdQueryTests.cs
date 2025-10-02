using FluentAssertions;
using GameGuild.Modules.UserProfiles;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Queries;

/// <summary>
/// Unit tests for GetUserProfileByUserIdQuery
/// </summary>
public class GetUserProfileByUserIdQueryTests
{
    [Fact]
    public void Query_Should_Have_Required_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserProfileByUserIdQuery
        {
            UserId = userId,
            IncludeDeleted = true
        };

        // Assert
        query.UserId.Should().Be(userId);
        query.IncludeDeleted.Should().BeTrue();
    }

    [Fact]
    public void Query_Should_Default_IncludeDeleted_To_False()
    {
        // Arrange & Act
        var query = new GetUserProfileByUserIdQuery
        {
            UserId = Guid.NewGuid()
        };

        // Assert
        query.IncludeDeleted.Should().BeFalse();
    }
}
