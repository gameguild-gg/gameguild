using FluentAssertions;
using GameGuild.Modules.Permissions;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Entities;

/// <summary>
/// Unit tests for UserPermission entity
/// </summary>
public class UserPermissionTests
{
    [Fact]
    public void UserPermission_Should_Have_Required_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var action = "read";
        var resourceType = "TestingSession";
        var resourceId = Guid.NewGuid();
        var grantedByRole = "Administrator";
        var expiresAt = DateTime.UtcNow.AddDays(30);

        // Act
        var userPermission = new UserPermission
        {
            UserId = userId,
            TenantId = tenantId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            GrantedByRole = grantedByRole,
            ExpiresAt = expiresAt,
            IsActive = true
        };

        // Assert
        userPermission.UserId.Should().Be(userId);
        userPermission.TenantId.Should().Be(tenantId);
        userPermission.Action.Should().Be(action);
        userPermission.ResourceType.Should().Be(resourceType);
        userPermission.ResourceId.Should().Be(resourceId);
        userPermission.GrantedByRole.Should().Be(grantedByRole);
        userPermission.ExpiresAt.Should().Be(expiresAt);
        userPermission.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UserPermission_Should_Default_IsActive_To_True()
    {
        // Arrange & Act
        var userPermission = new UserPermission();

        // Assert
        userPermission.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UserPermission_Should_Serialize_And_Deserialize_Constraints()
    {
        // Arrange
        var userPermission = new UserPermission
        {
            UserId = Guid.NewGuid(),
            Action = "read",
            ResourceType = "Project"
        };

        var constraints = new List<PermissionConstraint>
        {
            new() { Type = "time", Value = "business_hours" },
            new() { Type = "ip", Value = "192.168.1.0/24" }
        };

        // Act
        userPermission.Constraints = constraints;
        var retrievedConstraints = userPermission.Constraints;

        // Assert
        retrievedConstraints.Should().HaveCount(2);
        retrievedConstraints[0].Type.Should().Be("time");
        retrievedConstraints[0].Value.Should().Be("business_hours");
        retrievedConstraints[1].Type.Should().Be("ip");
        retrievedConstraints[1].Value.Should().Be("192.168.1.0/24");
    }

    [Fact]
    public void UserPermission_Should_Default_Constraints_To_Empty_List()
    {
        // Arrange & Act
        var userPermission = new UserPermission
        {
            UserId = Guid.NewGuid(),
            Action = "read",
            ResourceType = "Project"
        };

        // Assert
        userPermission.Constraints.Should().BeEmpty();
    }

    [Fact]
    public void UserPermission_Should_Handle_Null_ConstraintsJson()
    {
        // Arrange & Act
        var userPermission = new UserPermission
        {
            UserId = Guid.NewGuid(),
            Action = "read",
            ResourceType = "Project",
            ConstraintsJson = string.Empty
        };

        // Assert
        userPermission.Constraints.Should().BeEmpty();
    }
}
