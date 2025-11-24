using FluentAssertions;
using GameGuild.Permissions.Entities;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Entities;

/// <summary>
/// Unit tests for PermissionAuditLog entity
/// </summary>
public class PermissionAuditLogTests
{
    [Fact]
    public void PermissionAuditLog_Should_Have_Required_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var operation = "Grant";
        var permissions = new[] { PermissionType.Read };
        var reason = "Test audit";
        var performedBy = Guid.NewGuid();

        // Act
        var auditLog = new PermissionAuditLog
        {
            UserId = userId,
            TenantId = tenantId,
            ResourceId = resourceId,
            Operation = operation,
            Permissions = permissions,
            Reason = reason,
            PerformedBy = performedBy
        };

        // Assert
        auditLog.UserId.Should().Be(userId);
        auditLog.TenantId.Should().Be(tenantId);
        auditLog.ResourceId.Should().Be(resourceId);
        auditLog.Operation.Should().Be(operation);
        auditLog.Permissions.Should().BeEquivalentTo(permissions);
        auditLog.Reason.Should().Be(reason);
        auditLog.PerformedBy.Should().Be(performedBy);
    }

    [Fact]
    public void PermissionAuditLog_Should_Default_Permissions_To_Empty_Array()
    {
        // Arrange & Act
        var auditLog = new PermissionAuditLog();

        // Assert
        auditLog.Permissions.Should().BeEmpty();
    }
}
