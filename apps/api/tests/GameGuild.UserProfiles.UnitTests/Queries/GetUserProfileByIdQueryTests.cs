using FluentAssertions;
using GameGuild.UserProfiles;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Queries;

/// <summary>
/// Unit tests for GetUserProfileByIdQuery
/// </summary>
public class GetUserProfileByIdQueryTests
{
    [Fact]
    public void Query_Should_Have_Required_Properties()
    {
        // Arrange
        var userProfileId = Guid.NewGuid();

        // Act
        var query = new GetUserProfileByIdQuery
        {
            UserProfileId = userProfileId,
            IncludeDeleted = true
        };

        // Assert
        query.UserProfileId.Should().Be(userProfileId);
        query.IncludeDeleted.Should().BeTrue();
    }

    [Fact]
    public void Query_Should_Default_IncludeDeleted_To_False()
    {
        // Arrange & Act
        var query = new GetUserProfileByIdQuery
        {
            UserProfileId = Guid.NewGuid()
        };

        // Assert
        query.IncludeDeleted.Should().BeFalse();
    }
}
