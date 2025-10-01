using FluentAssertions;
using GameGuild.Modules.Permissions;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Entities;

/// <summary>
/// Unit tests for the TenantPermission entity
/// Tests entity behavior, validation, and business logic
/// </summary>
public class TenantPermissionTests
{
    [Fact]
    public void Constructor_ShouldCreateTenantPermission_WithValidParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var tenantPermission = new TenantPermission(userId, tenantId);

        // Assert
        tenantPermission.UserId.Should().Be(userId);
        tenantPermission.TenantId.Should().Be(tenantId);
        tenantPermission.Id.Should().NotBeEmpty();
        tenantPermission.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tenantPermission.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tenantPermission.DeletedAt.Should().BeNull();
        tenantPermission.IsActive.Should().BeTrue();
        tenantPermission.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateTenantPermission_WithNullUserId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var tenantPermission = new TenantPermission(null, tenantId);

        // Assert
        tenantPermission.UserId.Should().BeNull();
        tenantPermission.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Constructor_ShouldCreateTenantPermission_WithNullTenantId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var tenantPermission = new TenantPermission(userId, null);

        // Assert
        tenantPermission.UserId.Should().Be(userId);
        tenantPermission.TenantId.Should().BeNull();
    }

    [Fact]
    public void DefaultConstructor_ShouldCreateEmptyTenantPermission()
    {
        // Act
        var tenantPermission = new TenantPermission();

        // Assert
        tenantPermission.UserId.Should().BeNull();
        tenantPermission.TenantId.Should().BeNull();
        tenantPermission.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void AddPermission_ShouldAddSinglePermission()
    {
        // Arrange
        var tenantPermission = new TenantPermission(Guid.NewGuid(), Guid.NewGuid());

        // Act
        tenantPermission.AddPermission(PermissionType.Read);

        // Assert
        tenantPermission.HasPermission(PermissionType.Read).Should().BeTrue();
        tenantPermission.HasPermission(PermissionType.Comment).Should().BeFalse();
    }

    [Fact]
    public void AddPermission_ShouldAddMultiplePermissions()
    {
        // Arrange
        var tenantPermission = new TenantPermission(Guid.NewGuid(), Guid.NewGuid());

        // Act
        tenantPermission.AddPermission(PermissionType.Read);
        tenantPermission.AddPermission(PermissionType.Comment);
        tenantPermission.AddPermission(PermissionType.Vote);

        // Assert
        tenantPermission.HasPermission(PermissionType.Read).Should().BeTrue();
        tenantPermission.HasPermission(PermissionType.Comment).Should().BeTrue();
        tenantPermission.HasPermission(PermissionType.Vote).Should().BeTrue();
        tenantPermission.HasPermission(PermissionType.Share).Should().BeFalse();
    }

    [Fact]
    public void RemovePermission_ShouldRemoveSpecificPermission()
    {
        // Arrange
        var tenantPermission = new TenantPermission(Guid.NewGuid(), Guid.NewGuid());
        tenantPermission.AddPermission(PermissionType.Read);
        tenantPermission.AddPermission(PermissionType.Comment);

        // Act
        tenantPermission.RemovePermission(PermissionType.Read);

        // Assert
        tenantPermission.HasPermission(PermissionType.Read).Should().BeFalse();
        tenantPermission.HasPermission(PermissionType.Comment).Should().BeTrue();
    }

    [Fact]
    public void HasPermission_ShouldReturnFalse_WhenPermissionNotAdded()
    {
        // Arrange
        var tenantPermission = new TenantPermission(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        tenantPermission.HasPermission(PermissionType.Read).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenExpiresAtIsNull()
    {
        // Arrange
        var tenantPermission = new TenantPermission(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        tenantPermission.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenExpiresAtIsInFuture()
    {
        // Arrange
        var tenantPermission = new TenantPermission(Guid.NewGuid(), Guid.NewGuid())
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        // Act & Assert
        tenantPermission.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenExpiresAtIsInPast()
    {
        // Arrange
        var tenantPermission = new TenantPermission(Guid.NewGuid(), Guid.NewGuid())
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act & Assert
        tenantPermission.IsExpired.Should().BeTrue();
    }

    [Theory]
    [InlineData(PermissionType.Read)]
    [InlineData(PermissionType.Comment)]
    [InlineData(PermissionType.Vote)]
    [InlineData(PermissionType.Share)]
    [InlineData(PermissionType.Report)]
    public void AddPermission_ShouldWorkForAllPermissionTypes(PermissionType permissionType)
    {
        // Arrange
        var tenantPermission = new TenantPermission(Guid.NewGuid(), Guid.NewGuid());

        // Act
        tenantPermission.AddPermission(permissionType);

        // Assert
        tenantPermission.HasPermission(permissionType).Should().BeTrue();
    }

    [Fact]
    public void AddPermission_ShouldBeIdempotent()
    {
        // Arrange
        var tenantPermission = new TenantPermission(Guid.NewGuid(), Guid.NewGuid());

        // Act
        tenantPermission.AddPermission(PermissionType.Read);
        tenantPermission.AddPermission(PermissionType.Read); // Add same permission twice

        // Assert
        tenantPermission.HasPermission(PermissionType.Read).Should().BeTrue();
    }

    [Fact]
    public void RemovePermission_ShouldNotThrow_WhenPermissionNotPresent()
    {
        // Arrange
        var tenantPermission = new TenantPermission(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        var action = () => tenantPermission.RemovePermission(PermissionType.Read);
        action.Should().NotThrow();
        tenantPermission.HasPermission(PermissionType.Read).Should().BeFalse();
    }

    [Fact]
    public void TenantPermission_ShouldInheritFromWithPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var tenantPermission = new TenantPermission(userId, tenantId);

        // Assert
        tenantPermission.Should().BeAssignableTo<WithPermissions>();
    }
}