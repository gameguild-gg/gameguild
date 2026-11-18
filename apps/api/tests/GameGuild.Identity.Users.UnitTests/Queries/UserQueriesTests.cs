using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Queries;

/// <summary>
/// Tests for simple query record instantiation to ensure coverage
/// </summary>
public class UserQueriesTests
{
    [Fact]
    public void GetUsersPagedQuery_ShouldInstantiateWithDefaults()
    {
        // Act
        var query = new GetUsersPagedQuery();

        // Assert
        query.IsActive.Should().BeNull();
        query.PageNumber.Should().Be(1);
        query.PageSize.Should().Be(10);
    }

    [Fact]
    public void GetUsersPagedQuery_ShouldInstantiateWithCustomValues()
    {
        // Act
        var query = new GetUsersPagedQuery(IsActive: true, PageNumber: 2, PageSize: 20);

        // Assert
        query.IsActive.Should().BeTrue();
        query.PageNumber.Should().Be(2);
        query.PageSize.Should().Be(20);
    }

    [Fact]
    public void GetUsersQuery_ShouldInstantiateWithDefaults()
    {
        // Act
        var query = new GetUsersQuery();

        // Assert
        query.Email.Should().BeNull();
        query.Status.Should().BeNull();
        query.IncludeDeleted.Should().BeFalse();
        query.SearchTerm.Should().BeNull();
        query.Cursor.Should().BeNull();
        query.Limit.Should().Be(50);
        query.Sort.Should().BeNull();
    }

    [Fact]
    public void GetUserMetadataQuery_ShouldInstantiateWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserMetadataQuery(userId);

        // Assert
        query.UserId.Should().Be(userId);
    }

    [Fact]
    public void GetUserPreferencesQuery_ShouldInstantiateWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserPreferencesQuery(userId);

        // Assert
        query.UserId.Should().Be(userId);
    }

    [Fact]
    public void GetUserProfileQuery_ShouldInstantiateWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserProfileQuery(userId);

        // Assert
        query.UserId.Should().Be(userId);
    }
}
