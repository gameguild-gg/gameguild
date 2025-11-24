using FluentAssertions;
using GameGuild.Authentication.Entities;
using Xunit;

namespace GameGuild.Authentication.UnitTests.Entities;

public class RoleEntityTests
{
    [Fact]
    public void Role_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var role = new Role("Admin", "Administrator role", null);

        // Assert
        role.Name.Should().Be("Admin");
        role.Description.Should().Be("Administrator role");
        role.TenantId.Should().BeNull();
        role.IsActive.Should().BeTrue();
        role.Permissions.Should().Be("[]");
    }

    [Fact]
    public void Role_WithTenantId_SetsTenanIdCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var role = new Role("TenantRole", "Tenant specific role", tenantId);

        // Assert
        role.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void IsGlobalRole_WhenTenantIdIsNull_ReturnsTrue()
    {
        // Arrange
        var role = new Role("GlobalRole", "Global role", null);

        // Act
        var isGlobal = role.IsGlobalRole();

        // Assert
        isGlobal.Should().BeTrue();
    }

    [Fact]
    public void IsGlobalRole_WhenTenantIdIsSet_ReturnsFalse()
    {
        // Arrange
        var role = new Role("TenantRole", "Tenant role", Guid.NewGuid());

        // Act
        var isGlobal = role.IsGlobalRole();

        // Assert
        isGlobal.Should().BeFalse();
    }

    [Fact]
    public void IsTenantRole_WhenTenantIdIsSet_ReturnsTrue()
    {
        // Arrange
        var role = new Role("TenantRole", "Tenant role", Guid.NewGuid());

        // Act
        var isTenant = role.IsTenantRole();

        // Assert
        isTenant.Should().BeTrue();
    }

    [Fact]
    public void IsTenantRole_WhenTenantIdIsNull_ReturnsFalse()
    {
        // Arrange
        var role = new Role("GlobalRole", "Global role", null);

        // Act
        var isTenant = role.IsTenantRole();

        // Assert
        isTenant.Should().BeFalse();
    }
}
