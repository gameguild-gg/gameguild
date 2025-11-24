using FluentAssertions;
using GameGuild.UserProfiles;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Queries;

/// <summary>
/// Unit tests for GetAllUserProfilesQuery
/// </summary>
public class GetAllUserProfilesQueryTests
{
    [Fact]
    public void Query_Should_Have_Required_Properties()
    {
        // Arrange
        var searchTerm = "test";
        var tenantId = Guid.NewGuid();

        // Act
        var query = new GetAllUserProfilesQuery
        {
            IncludeDeleted = true,
            Skip = 10,
            Take = 25,
            SearchTerm = searchTerm,
            TenantId = tenantId
        };

        // Assert
        query.IncludeDeleted.Should().BeTrue();
        query.Skip.Should().Be(10);
        query.Take.Should().Be(25);
        query.SearchTerm.Should().Be(searchTerm);
        query.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Query_Should_Have_Default_Values()
    {
        // Arrange & Act
        var query = new GetAllUserProfilesQuery();

        // Assert
        query.IncludeDeleted.Should().BeFalse();
        query.Skip.Should().Be(0);
        query.Take.Should().Be(50);
    }
}
