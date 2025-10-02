using FluentAssertions;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Permissions.Queries;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Queries;

/// <summary>
/// Unit tests for GetTenantPermissionsQuery
/// </summary>
public class GetTenantPermissionsQueryTests
{
    [Fact]
    public void Query_Should_Have_Required_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var query = new GetTenantPermissionsQuery
        {
            UserId = userId,
            TenantId = tenantId,
            IncludeEffectivePermissions = false
        };

        // Assert
        query.UserId.Should().Be(userId);
        query.TenantId.Should().Be(tenantId);
        query.IncludeEffectivePermissions.Should().BeFalse();
    }

    [Fact]
    public void Query_Should_Default_IncludeEffectivePermissions_To_True()
    {
        // Arrange & Act
        var query = new GetTenantPermissionsQuery();

        // Assert
        query.IncludeEffectivePermissions.Should().BeTrue();
    }
}
