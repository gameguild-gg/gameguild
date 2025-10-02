using FluentAssertions;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the UserMfaConfiguration entity
/// Tests the properties and behavior of user MFA configuration
/// </summary>
public class UserMfaConfigurationTests
{
    [Fact]
    public void UserMfaConfiguration_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var config = new UserMfaConfiguration();

        // Assert
        config.IsEnabled.Should().BeFalse();
        config.TotpSecretKey.Should().BeNull();
        config.BackupCodes.Should().BeNull();
        config.EnabledAt.Should().BeNull();
        config.LastUsedAt.Should().BeNull();
        config.FailedAttempts.Should().Be(0);
        config.LockedOutUntil.Should().BeNull();
        config.PreferredMethod.Should().Be(MfaMethod.Totp);
    }

    [Fact]
    public void UserMfaConfiguration_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var enabledAt = DateTime.UtcNow;
        var lastUsedAt = DateTime.UtcNow;

        // Act
        var config = new UserMfaConfiguration
        {
            UserId = userId,
            IsEnabled = true,
            TotpSecretKey = "encrypted-secret-key",
            BackupCodes = "[\"code1\",\"code2\",\"code3\"]",
            EnabledAt = enabledAt,
            LastUsedAt = lastUsedAt,
            FailedAttempts = 0,
            LockedOutUntil = null,
            PreferredMethod = MfaMethod.Totp
        };

        // Assert
        config.UserId.Should().Be(userId);
        config.IsEnabled.Should().BeTrue();
        config.TotpSecretKey.Should().Be("encrypted-secret-key");
        config.BackupCodes.Should().Be("[\"code1\",\"code2\",\"code3\"]");
        config.EnabledAt.Should().Be(enabledAt);
        config.LastUsedAt.Should().Be(lastUsedAt);
        config.FailedAttempts.Should().Be(0);
        config.LockedOutUntil.Should().BeNull();
        config.PreferredMethod.Should().Be(MfaMethod.Totp);
    }

    [Fact]
    public void UserMfaConfiguration_ShouldHandleFailedAttempts()
    {
        // Arrange
        var lockedOutUntil = DateTime.UtcNow.AddMinutes(15);

        // Act
        var config = new UserMfaConfiguration
        {
            IsEnabled = true,
            FailedAttempts = 5,
            LockedOutUntil = lockedOutUntil
        };

        // Assert
        config.FailedAttempts.Should().Be(5);
        config.LockedOutUntil.Should().Be(lockedOutUntil);
    }

    [Fact]
    public void UserMfaConfiguration_ShouldSupportDifferentMfaMethods()
    {
        // Arrange & Act
        var totpConfig = new UserMfaConfiguration { PreferredMethod = MfaMethod.Totp };
        var smsConfig = new UserMfaConfiguration { PreferredMethod = MfaMethod.Sms };
        var emailConfig = new UserMfaConfiguration { PreferredMethod = MfaMethod.Email };

        // Assert
        totpConfig.PreferredMethod.Should().Be(MfaMethod.Totp);
        smsConfig.PreferredMethod.Should().Be(MfaMethod.Sms);
        emailConfig.PreferredMethod.Should().Be(MfaMethod.Email);
    }

    [Fact]
    public void UserMfaConfiguration_ShouldHandleDisabledMfa()
    {
        // Arrange & Act
        var config = new UserMfaConfiguration
        {
            IsEnabled = false,
            TotpSecretKey = null,
            EnabledAt = null,
            LastUsedAt = null
        };

        // Assert
        config.IsEnabled.Should().BeFalse();
        config.TotpSecretKey.Should().BeNull();
        config.EnabledAt.Should().BeNull();
        config.LastUsedAt.Should().BeNull();
    }
}
