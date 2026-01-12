using FluentAssertions;
using GameGuild.Identity.Users;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Entities;

public class UserProfileTests
{
    [Fact]
    public void Create_ShouldInitializeWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var profile = UserProfile.Create(userId);

        // Assert
        profile.Should().NotBeNull();
        profile.UserId.Should().Be(userId);
        profile.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void UpdateDisplayInfo_ShouldUpdateFields()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid());
        var displayName = "John Doe";
        var bio = "Software Developer";
        var location = "San Francisco";
        var website = "https://example.com";

        // Act
        profile.UpdateBasicInfo(displayName, bio, location, website);

        // Assert
        profile.DisplayName.Should().Be(displayName);
        profile.Bio.Should().Be(bio);
        profile.Location.Should().Be(location);
        profile.Website.Should().Be(website);
    }

    [Fact]
    public void UpdateWebsite_ShouldUpdateWebsiteField()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid());
        var website = "https://example.com";

        // Act
        profile.UpdateBasicInfo(website: website);

        // Assert
        profile.Website.Should().Be(website);
    }

    [Fact]
    public void UpdateAvatar_ShouldUpdateAvatarUrl()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid());
        var avatarUrl = "https://example.com/avatar.jpg";

        // Act
        profile.UpdateAvatar(avatarUrl);

        // Assert
        profile.AvatarUrl.Should().Be(avatarUrl);
    }

    [Fact]
    public void UpdateBanner_ShouldUpdateBannerUrl()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid());
        var bannerUrl = "https://example.com/banner.jpg";

        // Act
        profile.UpdateBanner(bannerUrl);

        // Assert
        profile.BannerUrl.Should().Be(bannerUrl);
    }

    [Fact]
    public void Touch_ShouldUpdateTimestamp()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid());
        var originalUpdateAt = profile.UpdatedAt;
        Thread.Sleep(10);

        // Act
        profile.Touch();

        // Assert
        profile.UpdatedAt.Should().BeAfter(originalUpdateAt);
    }

    [Fact]
    public void UpdateProfessionalInfo_ShouldUpdateProperties()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid());
        var jobTitle = "Senior Developer";
        var company = "Tech Corp";

        // Act
        profile.UpdateProfessionalInfo(jobTitle, company);

        // Assert
        profile.JobTitle.Should().Be(jobTitle);
        profile.Company.Should().Be(company);
    }

    [Fact]
    public void UpdateVisibility_ShouldUpdateVisibilityProperty()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid());

        // Act
        profile.UpdateVisibility(ProfileVisibility.Private);

        // Assert
        profile.Visibility.Should().Be(ProfileVisibility.Private);
    }

    [Fact]
    public void SetVerificationStatus_ShouldUpdateIsVerifiedProperty()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid());

        // Act
        profile.SetVerificationStatus(true);

        // Assert
        profile.IsVerified.Should().BeTrue();
    }

    [Fact]
    public void Create_WithDisplayName_ShouldSetDisplayName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var displayName = "John Doe";

        // Act
        var profile = UserProfile.Create(userId, displayName);

        // Assert
        profile.DisplayName.Should().Be(displayName);
    }

    [Fact]
    public void UpdateBasicInfo_ShouldUpdateAllFields()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid());
        var displayName = "New Name";
        var bio = "New Bio";
        var location = "New Location";
        var website = "https://newwebsite.com";

        // Act
        profile.UpdateBasicInfo(displayName, bio, location, website);

        // Assert
        profile.DisplayName.Should().Be(displayName);
        profile.Bio.Should().Be(bio);
        profile.Location.Should().Be(location);
        profile.Website.Should().Be(website);
    }

    [Fact]
    public void UpdateBasicInfo_WithNullValues_ShouldNotUpdate()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid());
        profile.DisplayName = "Original Name";
        profile.Bio = "Original Bio";

        // Act
        profile.UpdateBasicInfo(null, null, null, null);

        // Assert
        profile.DisplayName.Should().Be("Original Name");
        profile.Bio.Should().Be("Original Bio");
    }

    [Fact]
    public void Constructor_WithPartial_ShouldCallBase()
    {
        // Arrange
        var partial = new { };

        // Act
        var profile = new UserProfile(partial);

        // Assert
        profile.Should().NotBeNull();
        profile.Id.Should().NotBe(Guid.Empty);
    }
}
