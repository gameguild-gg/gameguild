using FluentAssertions;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the TrustedDevice entity
/// Tests the properties and behavior of trusted device management
/// </summary>
public class TrustedDeviceTests
{
    [Fact]
    public void TrustedDevice_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var device = new TrustedDevice();

        // Assert
        device.DeviceFingerprint.Should().BeEmpty();
        device.DeviceName.Should().BeEmpty();
        device.DeviceInfo.Should().BeEmpty();
        device.IsActive.Should().BeTrue();
        device.ExpiresAt.Should().BeNull();
        device.AssociatedIpAddresses.Should().BeNull();
    }

    [Fact]
    public void TrustedDevice_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var trustedAt = DateTime.UtcNow;
        var lastUsedAt = DateTime.UtcNow;
        var expiresAt = DateTime.UtcNow.AddDays(30);

        // Act
        var device = new TrustedDevice
        {
            UserId = userId,
            DeviceFingerprint = "fingerprint-hash-123",
            DeviceName = "iPhone 13 Pro",
            DeviceInfo = "{\"os\":\"iOS 17\",\"browser\":\"Safari\"}",
            TrustedAt = trustedAt,
            LastUsedAt = lastUsedAt,
            IsActive = true,
            ExpiresAt = expiresAt,
            AssociatedIpAddresses = "[\"192.168.1.1\",\"10.0.0.1\"]"
        };

        // Assert
        device.UserId.Should().Be(userId);
        device.DeviceFingerprint.Should().Be("fingerprint-hash-123");
        device.DeviceName.Should().Be("iPhone 13 Pro");
        device.DeviceInfo.Should().Be("{\"os\":\"iOS 17\",\"browser\":\"Safari\"}");
        device.TrustedAt.Should().Be(trustedAt);
        device.LastUsedAt.Should().Be(lastUsedAt);
        device.IsActive.Should().BeTrue();
        device.ExpiresAt.Should().Be(expiresAt);
        device.AssociatedIpAddresses.Should().Be("[\"192.168.1.1\",\"10.0.0.1\"]");
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenDeviceTrustIsExpired()
    {
        // Arrange
        var device = new TrustedDevice
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act & Assert
        device.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenDeviceTrustIsNotExpired()
    {
        // Arrange
        var device = new TrustedDevice
        {
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        // Act & Assert
        device.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenExpirationIsNull()
    {
        // Arrange
        var device = new TrustedDevice
        {
            ExpiresAt = null
        };

        // Act & Assert
        device.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void TrustedDevice_ShouldHandleInactiveDevice()
    {
        // Arrange & Act
        var device = new TrustedDevice
        {
            DeviceFingerprint = "old-device-hash",
            IsActive = false,
            LastUsedAt = DateTime.UtcNow.AddMonths(-6)
        };

        // Assert
        device.IsActive.Should().BeFalse();
    }
}
