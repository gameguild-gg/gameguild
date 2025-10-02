using FluentAssertions;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Permissions.Queries;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Queries;

/// <summary>
/// Unit tests for HasTenantPermissionQuery
/// </summary>
public class HasTenantPermissionQueryTests
{
    [Fact]
    public void Query_Should_Have_Required_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permission = PermissionType.Read;

        // Act
        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = permission
        };

        // Assert
        query.UserId.Should().Be(userId);
        query.TenantId.Should().Be(tenantId);
        query.Permission.Should().Be(permission);
    }
}
