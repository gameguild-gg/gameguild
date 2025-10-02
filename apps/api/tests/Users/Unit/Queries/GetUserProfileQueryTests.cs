using FluentAssertions;
using GameGuild.Modules.Users;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Queries;

/// <summary>
/// Unit tests for GetUserProfileQuery
/// </summary>
public class GetUserProfileQueryTests
{
    [Fact]
    public void Query_Should_Be_Creatable()
    {
        // Arrange & Act
        var query = new GetUserProfileQuery();

        // Assert
        query.Should().NotBeNull();
    }
}
