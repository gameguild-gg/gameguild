using FluentAssertions;
using GameGuild.Permissions.Commands;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Commands;

/// <summary>
/// Unit tests for GrantTenantPermissionCommand
/// </summary>
public class GrantTenantPermissionCommandTests
{
    [Fact]
    public void Command_Should_Have_Required_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permissions = new[] { PermissionType.Read, PermissionType.Edit };
        var reason = "Test grant";

        // Act
        var command = new GrantTenantPermissionCommand
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = permissions,
            Reason = reason
        };

        // Assert
        command.UserId.Should().Be(userId);
        command.TenantId.Should().Be(tenantId);
        command.Permissions.Should().BeEquivalentTo(permissions);
        command.Reason.Should().Be(reason);
    }

    [Fact]
    public void Command_Should_Default_Permissions_To_Empty_Array()
    {
        // Arrange & Act
        var command = new GrantTenantPermissionCommand();

        // Assert
        command.Permissions.Should().BeEmpty();
    }
}
