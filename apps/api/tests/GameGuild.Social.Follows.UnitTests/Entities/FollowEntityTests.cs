using FluentAssertions;
using Xunit;

namespace GameGuild.Social.Follows.Tests;

/// <summary>
/// Unit tests for Follow entity domain logic.
/// </summary>
public class FollowEntityTests
{
    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        var followerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var follow = Follow.Create(followerId, entityId, "User");

        follow.Id.Should().NotBeEmpty();
        follow.FollowerId.Should().Be(followerId);
        follow.FollowedEntityId.Should().Be(entityId);
        follow.FollowedEntityType.Should().Be("User");
        follow.NotificationsEnabled.Should().BeTrue();
        follow.FollowedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithNotificationsDisabled_ShouldSetFalse()
    {
        var follow = Follow.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", notificationsEnabled: false);
        follow.NotificationsEnabled.Should().BeFalse();
    }

    [Fact]
    public void EnableNotifications_ShouldSetTrue()
    {
        var follow = Follow.Create(Guid.NewGuid(), Guid.NewGuid(), "User", notificationsEnabled: false);
        follow.EnableNotifications();
        follow.NotificationsEnabled.Should().BeTrue();
    }

    [Fact]
    public void DisableNotifications_ShouldSetFalse()
    {
        var follow = Follow.Create(Guid.NewGuid(), Guid.NewGuid(), "User");
        follow.DisableNotifications();
        follow.NotificationsEnabled.Should().BeFalse();
    }

    [Fact]
    public void UpdateNotificationSettings_ShouldToggle()
    {
        var follow = Follow.Create(Guid.NewGuid(), Guid.NewGuid(), "User");
        follow.UpdateNotificationSettings(false);
        follow.NotificationsEnabled.Should().BeFalse();
        follow.UpdateNotificationSettings(true);
        follow.NotificationsEnabled.Should().BeTrue();
    }
}

/// <summary>
/// Unit tests for Block entity domain logic.
/// </summary>
public class BlockEntityTests
{
    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        var block = Block.Create(blockerId, blockedId, "Spam");

        block.Id.Should().NotBeEmpty();
        block.BlockerId.Should().Be(blockerId);
        block.BlockedId.Should().Be(blockedId);
        block.Reason.Should().Be("Spam");
        block.BlockedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithoutReason_ShouldBeNull()
    {
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid());
        block.Reason.Should().BeNull();
    }
}

/// <summary>
/// Unit tests for Mute entity domain logic.
/// </summary>
public class MuteEntityTests
{
    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        var mute = Mute.Create(muterId, mutedId, "Annoying", DateTime.UtcNow.AddDays(7));

        mute.Id.Should().NotBeEmpty();
        mute.MuterId.Should().Be(muterId);
        mute.MutedId.Should().Be(mutedId);
        mute.Reason.Should().Be("Annoying");
        mute.ExpiresAt.Should().NotBeNull();
        mute.MutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void IsExpired_WhenNotExpired_ShouldReturnFalse()
    {
        var mute = Mute.Create(Guid.NewGuid(), Guid.NewGuid(), expiresAt: DateTime.UtcNow.AddDays(1));
        mute.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpired_ShouldReturnTrue()
    {
        var mute = Mute.Create(Guid.NewGuid(), Guid.NewGuid(), expiresAt: DateTime.UtcNow.AddDays(-1));
        mute.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenNoExpiration_ShouldReturnFalse()
    {
        var mute = Mute.Create(Guid.NewGuid(), Guid.NewGuid());
        mute.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void ExtendExpiration_ShouldUpdateExpiresAt()
    {
        var mute = Mute.Create(Guid.NewGuid(), Guid.NewGuid(), expiresAt: DateTime.UtcNow.AddDays(1));
        var newExpiry = DateTime.UtcNow.AddDays(30);
        mute.ExtendExpiration(newExpiry);
        mute.ExpiresAt.Should().Be(newExpiry);
    }
}

/// <summary>
/// Unit tests for FollowPrivacySettings entity domain logic.
/// </summary>
public class FollowPrivacySettingsTests
{
    [Fact]
    public void CreateDefault_ShouldEnableAllDefaults()
    {
        var userId = Guid.NewGuid();
        var settings = FollowPrivacySettings.CreateDefault(userId);

        settings.UserId.Should().Be(userId);
        settings.IsFollowerListPublic.Should().BeTrue();
        settings.IsFollowingListPublic.Should().BeTrue();
        settings.AllowFollowers.Should().BeTrue();
        settings.NotifyOnNewFollower.Should().BeTrue();
        settings.ShowFollowerCount.Should().BeTrue();
        settings.ShowFollowingCount.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldChangeAllSettings()
    {
        var settings = FollowPrivacySettings.CreateDefault(Guid.NewGuid());

        settings.Update(
            isFollowerListPublic: false,
            isFollowingListPublic: false,
            allowFollowers: false,
            notifyOnNewFollower: false,
            showFollowerCount: false,
            showFollowingCount: false);

        settings.IsFollowerListPublic.Should().BeFalse();
        settings.IsFollowingListPublic.Should().BeFalse();
        settings.AllowFollowers.Should().BeFalse();
        settings.NotifyOnNewFollower.Should().BeFalse();
        settings.ShowFollowerCount.Should().BeFalse();
        settings.ShowFollowingCount.Should().BeFalse();
    }
}
