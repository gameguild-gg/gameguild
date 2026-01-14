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
    public void GetUserCustomFieldsQuery_ShouldInstantiateWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserCustomFieldsQuery(userId);

        // Assert
        query.UserId.Should().Be(userId);
    }

    [Fact]
    public void GetUserTagsQuery_ShouldInstantiateWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserTagsQuery(userId);

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
    public void GetUserNotificationPreferencesQuery_ShouldInstantiateWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserNotificationPreferencesQuery(userId);

        // Assert
        query.UserId.Should().Be(userId);
    }

    [Fact]
    public void GetUserAccessibilityPreferencesQuery_ShouldInstantiateWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserAccessibilityPreferencesQuery(userId);

        // Assert
        query.UserId.Should().Be(userId);
    }

    [Fact]
    public void GetUserPrivacyPreferencesQuery_ShouldInstantiateWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserPrivacyPreferencesQuery(userId);

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

    [Fact]
    [Trait("Category", "Skipped")]
    public void GetUserAvatarQuery_ShouldInstantiateWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserAvatarQuery(userId);

        // Assert
        query.UserId.Should().Be(userId);
    }

    [Fact]
    [Trait("Category", "Skipped")]
    public void GetUserBannerQuery_ShouldInstantiateWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserBannerQuery(userId);

        // Assert
        query.UserId.Should().Be(userId);
    }



    [Fact]
    public void GetUserNotificationsPageQuery_ShouldInstantiateWithAllParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;

        // Act
        var query = new GetUserNotificationsPageQuery(
            userId,
            Page: 1,
            PageSize: 20,
            Type: "Info",
            IsRead: false,
            Priority: "High",
            FromDate: fromDate,
            ToDate: toDate
        );

        // Assert
        query.UserId.Should().Be(userId);
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(20);
        query.Type.Should().Be("Info");
        query.IsRead.Should().BeFalse();
        query.Priority.Should().Be("High");
        query.FromDate.Should().Be(fromDate);
        query.ToDate.Should().Be(toDate);
    }

    [Fact]
    public void GetUserNotificationCountQuery_ShouldInstantiateWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var query = new GetUserNotificationCountQuery(userId);

        // Assert
        query.UserId.Should().Be(userId);
    }
}
