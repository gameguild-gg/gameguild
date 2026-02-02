using FluentAssertions;
using GameGuild.Compliance.Audit;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Entities;

/// <summary>
/// Unit tests for TamperEvidentAuditLog entity
/// </summary>
public class TamperEvidentAuditLogTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var previousHash = "prev-hash-123";

        // Act
        var log = TamperEvidentAuditLog.Create(
            tenantId,
            userId,
            "User.Login",
            "User",
            Guid.NewGuid(),
            null,
            "{\"status\":\"logged_in\"}",
            "{\"field\":\"status\",\"old\":null,\"new\":\"logged_in\"}",
            "Medium",
            "192.168.1.1",
            "Mozilla/5.0",
            "US",
            "California",
            "San Francisco",
            previousHash,
            1);

        // Assert
        log.Should().NotBeNull();
        log.TenantId.Should().Be(tenantId);
        log.UserId.Should().Be(userId);
        log.Action.Should().Be("User.Login");
        log.EntityType.Should().Be("User");
        log.RiskLevel.Should().Be("Medium");
        log.IpAddress.Should().Be("192.168.1.1");
        log.PreviousHash.Should().Be(previousHash);
        log.SequenceNumber.Should().Be(1);
        log.IsVerified.Should().BeFalse();
        log.ForwardedToSiem.Should().BeFalse();
        log.IsPartOfEvidence.Should().BeFalse();
    }

    [Fact]
    public void Create_WithNullUserId_ShouldCreateInstance()
    {
        // Act
        var log = TamperEvidentAuditLog.Create(
            Guid.NewGuid(),
            null,
            "System.Startup",
            "System",
            null,
            null,
            null,
            "{}",
            "Low",
            "127.0.0.1",
            "SystemAgent",
            null,
            null,
            null,
            "genesis",
            0);

        // Assert
        log.UserId.Should().BeNull();
        log.EntityId.Should().BeNull();
    }

    [Fact]
    public void SetCryptographicHashes_ShouldUpdateHashes()
    {
        // Arrange
        var log = CreateTestLog();
        var contentHash = "content-hash-abc";
        var chainHash = "chain-hash-xyz";

        // Act
        log.SetCryptographicHashes(contentHash, chainHash);

        // Assert
        log.ContentHash.Should().Be(contentHash);
        log.ChainHash.Should().Be(chainHash);
    }

    private static TamperEvidentAuditLog CreateTestLog()
    {
        return TamperEvidentAuditLog.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test.Action",
            "TestEntity",
            Guid.NewGuid(),
            null,
            "{}",
            "{}",
            "Low",
            "127.0.0.1",
            "TestAgent",
            null,
            null,
            null,
            "prev-hash",
            1);
    }
}

/// <summary>
/// Unit tests for AuditAnomaly entity
/// </summary>
public class AuditAnomalyTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var anomaly = AuditAnomaly.Create(
            tenantId,
            userId,
            AnomalyType.UnusualAccessPattern,
            AnomalySeverity.High,
            "Suspicious Login Pattern",
            "Multiple failed login attempts from different IPs",
            "RuleBasedDetection",
            0.95,
            "203.0.113.50",
            "{\"failedAttempts\":5}");

        // Assert
        anomaly.Should().NotBeNull();
        anomaly.TenantId.Should().Be(tenantId);
        anomaly.UserId.Should().Be(userId);
        anomaly.Type.Should().Be(AnomalyType.UnusualAccessPattern);
        anomaly.Severity.Should().Be(AnomalySeverity.High);
        anomaly.Title.Should().Be("Suspicious Login Pattern");
        anomaly.DetectionMethod.Should().Be("RuleBasedDetection");
        anomaly.ConfidenceScore.Should().Be(0.95);
        anomaly.Status.Should().Be(AnomalyStatus.Detected);
    }

    [Fact]
    public void Create_WithNullUserId_ShouldCreateInstance()
    {
        // Act
        var anomaly = AuditAnomaly.Create(
            Guid.NewGuid(),
            null,
            AnomalyType.TimeBasedAnomaly,
            AnomalySeverity.Medium,
            "System Anomaly",
            "Unexpected system behavior",
            "MLDetection",
            0.75,
            "10.0.0.1",
            "{}");

        // Assert
        anomaly.UserId.Should().BeNull();
    }

    [Fact]
    public void SetGeographicContext_ShouldUpdateLocation()
    {
        // Arrange
        var anomaly = CreateTestAnomaly();

        // Act
        anomaly.SetGeographicContext("US", "California", "San Francisco", 37.7749, -122.4194, false, 0.0);

        // Assert
        anomaly.Country.Should().Be("US");
        anomaly.Region.Should().Be("California");
        anomaly.City.Should().Be("San Francisco");
        anomaly.Latitude.Should().Be(37.7749);
        anomaly.Longitude.Should().Be(-122.4194);
        anomaly.IsSuspiciousLocation.Should().BeFalse();
    }

    [Fact]
    public void SetGeographicContext_WithSuspiciousLocation_ShouldMarkAsSuspicious()
    {
        // Arrange
        var anomaly = CreateTestAnomaly();

        // Act
        anomaly.SetGeographicContext("RU", "Moscow", "Moscow", 55.7558, 37.6173, true, 8000.0);

        // Assert
        anomaly.IsSuspiciousLocation.Should().BeTrue();
        anomaly.DistanceFromLastLogin.Should().Be(8000.0);
    }

    [Fact]
    public void SetDetectionDetails_ShouldUpdateRuleAndPattern()
    {
        // Arrange
        var anomaly = CreateTestAnomaly();

        // Act
        anomaly.SetDetectionDetails("BruteForceRule", "5+ failed logins in 5 minutes");

        // Assert
        anomaly.DetectionRule.Should().Be("BruteForceRule");
        anomaly.PatternMatched.Should().Be("5+ failed logins in 5 minutes");
    }

    private static AuditAnomaly CreateTestAnomaly()
    {
        return AuditAnomaly.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AnomalyType.GeographicAnomaly,
            AnomalySeverity.Medium,
            "Test Anomaly",
            "Test description",
            "TestMethod",
            0.8,
            "127.0.0.1",
            "{}");
    }
}
