using FluentAssertions;
using GameGuild.Compliance.Audit;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Entities;

/// <summary>
/// Unit tests for AuditLogDto
/// </summary>
public class AuditLogDtoTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaultValues()
    {
        // Act
        var dto = new AuditLogDto();

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().BeEmpty();
        dto.ActionType.Should().Be(string.Empty);
        dto.ResourceType.Should().Be(string.Empty);
        dto.ResourceId.Should().BeNull();
        dto.UserId.Should().BeNull();
        dto.TenantId.Should().BeNull();
        dto.IpAddress.Should().BeNull();
        dto.UserAgent.Should().BeNull();
        dto.SessionId.Should().BeNull();
        dto.Description.Should().BeNull();
        dto.Success.Should().BeFalse();
        dto.ErrorMessage.Should().BeNull();
        dto.RiskLevel.Should().Be(AuditRiskLevel.Low);
        dto.Category.Should().Be(AuditCategory.General);
        dto.CorrelationId.Should().BeNull();
        dto.CreatedAt.Should().Be(default);
    }

    [Fact]
    public void SetProperties_ShouldUpdateValues()
    {
        // Arrange
        var dto = new AuditLogDto();
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        dto.Id = id;
        dto.ActionType = "User.Login";
        dto.ResourceType = "User";
        dto.ResourceId = "123";
        dto.UserId = userId;
        dto.TenantId = tenantId;
        dto.IpAddress = "192.168.1.1";
        dto.UserAgent = "Mozilla/5.0";
        dto.SessionId = sessionId;
        dto.Description = "User logged in successfully";
        dto.Success = true;
        dto.ErrorMessage = null;
        dto.RiskLevel = AuditRiskLevel.Medium;
        dto.Category = AuditCategory.Authentication;
        dto.CorrelationId = "corr-123";
        dto.CreatedAt = createdAt;

        // Assert
        dto.Id.Should().Be(id);
        dto.ActionType.Should().Be("User.Login");
        dto.ResourceType.Should().Be("User");
        dto.ResourceId.Should().Be("123");
        dto.UserId.Should().Be(userId);
        dto.TenantId.Should().Be(tenantId);
        dto.IpAddress.Should().Be("192.168.1.1");
        dto.UserAgent.Should().Be("Mozilla/5.0");
        dto.SessionId.Should().Be(sessionId);
        dto.Description.Should().Be("User logged in successfully");
        dto.Success.Should().BeTrue();
        dto.ErrorMessage.Should().BeNull();
        dto.RiskLevel.Should().Be(AuditRiskLevel.Medium);
        dto.Category.Should().Be(AuditCategory.Authentication);
        dto.CorrelationId.Should().Be("corr-123");
        dto.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void ActionType_ShouldAcceptVariousActions()
    {
        // Arrange
        var dto = new AuditLogDto();
        var actions = new[]
        {
            "User.Create",
            "User.Update",
            "Permission.Granted",
            "Role.Assigned",
            "System.Shutdown"
        };

        // Act & Assert
        foreach (var action in actions)
        {
            dto.ActionType = action;
            dto.ActionType.Should().Be(action);
        }
    }

    [Fact]
    public void ResourceType_ShouldAcceptDifferentTypes()
    {
        // Arrange
        var dto = new AuditLogDto();
        var resourceTypes = new[] { "User", "Permission", "Role", "System", "Post" };

        // Act & Assert
        foreach (var resourceType in resourceTypes)
        {
            dto.ResourceType = resourceType;
            dto.ResourceType.Should().Be(resourceType);
        }
    }

    [Fact]
    public void Success_AndErrorMessage_ShouldTrackOperationResult()
    {
        // Arrange - Successful operation
        var successDto = new AuditLogDto
        {
            ActionType = "User.Login",
            ResourceType = "User",
            Success = true,
            ErrorMessage = null
        };

        // Arrange - Failed operation
        var failedDto = new AuditLogDto
        {
            ActionType = "User.Login",
            ResourceType = "User",
            Success = false,
            ErrorMessage = "Invalid credentials"
        };

        // Assert
        successDto.Success.Should().BeTrue();
        successDto.ErrorMessage.Should().BeNull();

        failedDto.Success.Should().BeFalse();
        failedDto.ErrorMessage.Should().Be("Invalid credentials");
    }

    [Theory]
    [InlineData(AuditRiskLevel.Low)]
    [InlineData(AuditRiskLevel.Medium)]
    [InlineData(AuditRiskLevel.High)]
    [InlineData(AuditRiskLevel.Critical)]
    public void RiskLevel_ShouldAcceptAllValues(AuditRiskLevel riskLevel)
    {
        // Arrange
        var dto = new AuditLogDto();

        // Act
        dto.RiskLevel = riskLevel;

        // Assert
        dto.RiskLevel.Should().Be(riskLevel);
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
        var dto = new AuditLogDto();

        // Act
        dto.Category = category;

        // Assert
        dto.Category.Should().Be(category);
    }

    [Fact]
    public void OptionalFields_ShouldSupportNull()
    {
        // Arrange & Act
        var dto = new AuditLogDto
        {
            ActionType = "System.Cleanup",
            ResourceType = "System",
            ResourceId = null,
            UserId = null,
            TenantId = null,
            IpAddress = null,
            UserAgent = null,
            SessionId = null,
            Description = null,
            ErrorMessage = null,
            CorrelationId = null
        };

        // Assert
        dto.ResourceId.Should().BeNull();
        dto.UserId.Should().BeNull();
        dto.TenantId.Should().BeNull();
        dto.IpAddress.Should().BeNull();
        dto.UserAgent.Should().BeNull();
        dto.SessionId.Should().BeNull();
        dto.Description.Should().BeNull();
        dto.ErrorMessage.Should().BeNull();
        dto.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void IpAddress_ShouldStoreIPv4AndIPv6()
    {
        // Arrange
        var dto = new AuditLogDto();

        // Act & Assert - IPv4
        dto.IpAddress = "192.168.1.1";
        dto.IpAddress.Should().Be("192.168.1.1");

        // Act & Assert - IPv6
        dto.IpAddress = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";
        dto.IpAddress.Should().Be("2001:0db8:85a3:0000:0000:8a2e:0370:7334");
    }

    [Fact]
    public void CorrelationId_ShouldLinkRelatedOperations()
    {
        // Arrange
        var correlationId = "corr-" + Guid.NewGuid();
        var dto1 = new AuditLogDto
        {
            ActionType = "Order.Create",
            ResourceType = "Order",
            CorrelationId = correlationId
        };

        var dto2 = new AuditLogDto
        {
            ActionType = "Payment.Process",
            ResourceType = "Payment",
            CorrelationId = correlationId
        };

        // Assert
        dto1.CorrelationId.Should().Be(correlationId);
        dto2.CorrelationId.Should().Be(correlationId);
        dto1.CorrelationId.Should().Be(dto2.CorrelationId);
    }

    [Fact]
    public void CreatedAt_ShouldStoreTimestamp()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var dto = new AuditLogDto();

        // Act
        dto.CreatedAt = timestamp;

        // Assert
        dto.CreatedAt.Should().Be(timestamp);
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AuditLogDto_ShouldSupportCompleteAuditData()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        // Act
        var dto = new AuditLogDto
        {
            Id = id,
            ActionType = "Permission.Granted",
            ResourceType = "Permission",
            ResourceId = "perm-admin",
            UserId = userId,
            TenantId = tenantId,
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0",
            SessionId = sessionId,
            Description = "Admin permission granted",
            Success = true,
            ErrorMessage = null,
            RiskLevel = AuditRiskLevel.High,
            Category = AuditCategory.Authorization,
            CorrelationId = "corr-security-123",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        dto.Id.Should().Be(id);
        dto.ActionType.Should().Be("Permission.Granted");
        dto.ResourceType.Should().Be("Permission");
        dto.ResourceId.Should().Be("perm-admin");
        dto.UserId.Should().Be(userId);
        dto.TenantId.Should().Be(tenantId);
        dto.IpAddress.Should().Be("192.168.1.100");
        dto.UserAgent.Should().Be("Mozilla/5.0");
        dto.SessionId.Should().Be(sessionId);
        dto.Description.Should().Be("Admin permission granted");
        dto.Success.Should().BeTrue();
        dto.ErrorMessage.Should().BeNull();
        dto.RiskLevel.Should().Be(AuditRiskLevel.High);
        dto.Category.Should().Be(AuditCategory.Authorization);
        dto.CorrelationId.Should().Be("corr-security-123");
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
