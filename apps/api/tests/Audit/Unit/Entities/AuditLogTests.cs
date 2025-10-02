using FluentAssertions;
using GameGuild.Modules.Audit;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Entities;

/// <summary>
/// Unit tests for AuditLog entity
/// </summary>
public class AuditLogTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaultValues()
    {
        // Act
        var auditLog = new AuditLog();

        // Assert
        auditLog.Should().NotBeNull();
        auditLog.Id.Should().NotBeEmpty(); // EntityBase auto-generates a new Guid
        auditLog.ActionType.Should().Be(string.Empty);
        auditLog.ResourceType.Should().Be(string.Empty);
        auditLog.ResourceId.Should().BeNull();
        auditLog.UserId.Should().BeNull();
        auditLog.TenantId.Should().BeNull();
        auditLog.IpAddress.Should().BeNull();
        auditLog.UserAgent.Should().BeNull();
        auditLog.SessionId.Should().BeNull();
        auditLog.Description.Should().BeNull();
        auditLog.Metadata.Should().BeNull();
        auditLog.Success.Should().BeFalse();
        auditLog.ErrorMessage.Should().BeNull();
        auditLog.RiskLevel.Should().Be(AuditRiskLevel.Low);
        auditLog.Category.Should().Be(AuditCategory.General);
        auditLog.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void SetProperties_ShouldUpdateValues()
    {
        // Arrange
        var auditLog = new AuditLog();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        // Act
        auditLog.ActionType = "User.Login";
        auditLog.ResourceType = "User";
        auditLog.ResourceId = "123";
        auditLog.UserId = userId;
        auditLog.TenantId = tenantId;
        auditLog.IpAddress = "192.168.1.1";
        auditLog.UserAgent = "Mozilla/5.0";
        auditLog.SessionId = sessionId;
        auditLog.Description = "User logged in successfully";
        auditLog.Metadata = "{\"email\":\"test@example.com\"}";
        auditLog.Success = true;
        auditLog.RiskLevel = AuditRiskLevel.Medium;
        auditLog.Category = AuditCategory.Authentication;
        auditLog.CorrelationId = "corr-123";

        // Assert
        auditLog.ActionType.Should().Be("User.Login");
        auditLog.ResourceType.Should().Be("User");
        auditLog.ResourceId.Should().Be("123");
        auditLog.UserId.Should().Be(userId);
        auditLog.TenantId.Should().Be(tenantId);
        auditLog.IpAddress.Should().Be("192.168.1.1");
        auditLog.UserAgent.Should().Be("Mozilla/5.0");
        auditLog.SessionId.Should().Be(sessionId);
        auditLog.Description.Should().Be("User logged in successfully");
        auditLog.Metadata.Should().Be("{\"email\":\"test@example.com\"}");
        auditLog.Success.Should().BeTrue();
        auditLog.RiskLevel.Should().Be(AuditRiskLevel.Medium);
        auditLog.Category.Should().Be(AuditCategory.Authentication);
        auditLog.CorrelationId.Should().Be("corr-123");
    }

    [Fact]
    public void ActionType_ShouldAcceptValidActions()
    {
        // Arrange
        var auditLog = new AuditLog();
        var validActions = new[]
        {
            "User.Login",
            "User.Logout",
            "User.Create",
            "Permission.Granted",
            "Role.Assigned"
        };

        // Act & Assert
        foreach (var action in validActions)
        {
            auditLog.ActionType = action;
            auditLog.ActionType.Should().Be(action);
        }
    }

    [Fact]
    public void ResourceType_ShouldAcceptDifferentTypes()
    {
        // Arrange
        var auditLog = new AuditLog();
        var resourceTypes = new[] { "User", "Tenant", "Permission", "Role", "Post" };

        // Act & Assert
        foreach (var resourceType in resourceTypes)
        {
            auditLog.ResourceType = resourceType;
            auditLog.ResourceType.Should().Be(resourceType);
        }
    }

    [Fact]
    public void Metadata_ShouldStoreJsonString()
    {
        // Arrange
        var auditLog = new AuditLog();
        var jsonMetadata = "{\"field\":\"value\",\"oldValue\":\"old\",\"newValue\":\"new\"}";

        // Act
        auditLog.Metadata = jsonMetadata;

        // Assert
        auditLog.Metadata.Should().Be(jsonMetadata);
    }

    [Fact]
    public void UserId_ShouldBeOptional()
    {
        // Arrange & Act
        var auditLog = new AuditLog
        {
            ActionType = "System.Cleanup",
            ResourceType = "System"
        };

        // Assert
        auditLog.UserId.Should().BeNull();
    }

    [Fact]
    public void TenantId_ShouldBeOptional()
    {
        // Arrange & Act
        var auditLog = new AuditLog
        {
            ActionType = "System.Maintenance",
            ResourceType = "System"
        };

        // Assert
        auditLog.TenantId.Should().BeNull();
    }

    [Fact]
    public void ResourceId_ShouldStoreAsString()
    {
        // Arrange
        var auditLog = new AuditLog();
        var guidId = Guid.NewGuid();
        var stringId = "user-123";
        var numericId = "456";

        // Act & Assert
        auditLog.ResourceId = guidId.ToString();
        auditLog.ResourceId.Should().Be(guidId.ToString());

        auditLog.ResourceId = stringId;
        auditLog.ResourceId.Should().Be(stringId);

        auditLog.ResourceId = numericId;
        auditLog.ResourceId.Should().Be(numericId);
    }

    [Theory]
    [InlineData(AuditRiskLevel.Low)]
    [InlineData(AuditRiskLevel.Medium)]
    [InlineData(AuditRiskLevel.High)]
    [InlineData(AuditRiskLevel.Critical)]
    public void RiskLevel_ShouldAcceptAllValues(AuditRiskLevel riskLevel)
    {
        // Arrange
        var auditLog = new AuditLog();

        // Act
        auditLog.RiskLevel = riskLevel;

        // Assert
        auditLog.RiskLevel.Should().Be(riskLevel);
    }

    [Theory]
    [InlineData(AuditCategory.General)]
    [InlineData(AuditCategory.Authentication)]
    [InlineData(AuditCategory.Authorization)]
    [InlineData(AuditCategory.Permission)]
    [InlineData(AuditCategory.User)]
    [InlineData(AuditCategory.Admin)]
    [InlineData(AuditCategory.Security)]
    [InlineData(AuditCategory.Data)]
    [InlineData(AuditCategory.System)]
    [InlineData(AuditCategory.Tenant)]
    [InlineData(AuditCategory.Privacy)]
    public void Category_ShouldAcceptAllValues(AuditCategory category)
    {
        // Arrange
        var auditLog = new AuditLog();

        // Act
        auditLog.Category = category;

        // Assert
        auditLog.Category.Should().Be(category);
    }

    [Fact]
    public void Success_ShouldTrackOperationResult()
    {
        // Arrange
        var successfulLog = new AuditLog
        {
            ActionType = "User.Login",
            ResourceType = "User",
            Success = true
        };

        var failedLog = new AuditLog
        {
            ActionType = "User.Login",
            ResourceType = "User",
            Success = false,
            ErrorMessage = "Invalid credentials"
        };

        // Assert
        successfulLog.Success.Should().BeTrue();
        successfulLog.ErrorMessage.Should().BeNull();

        failedLog.Success.Should().BeFalse();
        failedLog.ErrorMessage.Should().Be("Invalid credentials");
    }

    [Fact]
    public void IpAddress_ShouldStoreIPv4AndIPv6()
    {
        // Arrange
        var auditLog = new AuditLog();

        // Act & Assert - IPv4
        auditLog.IpAddress = "192.168.1.1";
        auditLog.IpAddress.Should().Be("192.168.1.1");

        // Act & Assert - IPv6
        auditLog.IpAddress = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";
        auditLog.IpAddress.Should().Be("2001:0db8:85a3:0000:0000:8a2e:0370:7334");
    }

    [Fact]
    public void CorrelationId_ShouldLinkRelatedOperations()
    {
        // Arrange
        var correlationId = "corr-" + Guid.NewGuid();
        var log1 = new AuditLog
        {
            ActionType = "Order.Create",
            ResourceType = "Order",
            CorrelationId = correlationId
        };

        var log2 = new AuditLog
        {
            ActionType = "Payment.Process",
            ResourceType = "Payment",
            CorrelationId = correlationId
        };

        // Assert
        log1.CorrelationId.Should().Be(correlationId);
        log2.CorrelationId.Should().Be(correlationId);
        log1.CorrelationId.Should().Be(log2.CorrelationId);
    }

    [Fact]
    public void AuditLog_ShouldSupportCompleteAuditTrail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        // Act
        var auditLog = new AuditLog
        {
            ActionType = "Permission.Granted",
            ResourceType = "Permission",
            ResourceId = "perm-admin",
            UserId = userId,
            TenantId = tenantId,
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0",
            SessionId = sessionId,
            Description = "Admin permission granted to user",
            Metadata = "{\"permission\":\"admin\",\"scope\":\"tenant\"}",
            Success = true,
            RiskLevel = AuditRiskLevel.High,
            Category = AuditCategory.Authorization,
            CorrelationId = "corr-security-123"
        };

        // Assert
        auditLog.ActionType.Should().Be("Permission.Granted");
        auditLog.ResourceType.Should().Be("Permission");
        auditLog.ResourceId.Should().Be("perm-admin");
        auditLog.UserId.Should().Be(userId);
        auditLog.TenantId.Should().Be(tenantId);
        auditLog.IpAddress.Should().Be("192.168.1.100");
        auditLog.UserAgent.Should().Be("Mozilla/5.0");
        auditLog.SessionId.Should().Be(sessionId);
        auditLog.Description.Should().Be("Admin permission granted to user");
        auditLog.Metadata.Should().Contain("admin");
        auditLog.Success.Should().BeTrue();
        auditLog.ErrorMessage.Should().BeNull();
        auditLog.RiskLevel.Should().Be(AuditRiskLevel.High);
        auditLog.Category.Should().Be(AuditCategory.Authorization);
        auditLog.CorrelationId.Should().Be("corr-security-123");
    }
}
