using FluentAssertions;
using GameGuild.Authentication.Entities;
using Xunit;

namespace GameGuild.Authentication.UnitTests.Entities;

public class UserRoleEntityTests
{
    [Fact]
    public void UserRole_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        // Act
        var userRole = new UserRole(userId, roleId, assignedBy);

        // Assert
        userRole.UserId.Should().Be(userId);
        userRole.RoleId.Should().Be(roleId);
        userRole.AssignedBy.Should().Be(assignedBy);
        userRole.AssignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        userRole.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void UserRole_WithoutAssignedBy_SetsNull()
    {
        // Arrange & Act
        var userRole = new UserRole(Guid.NewGuid(), Guid.NewGuid(), null);

        // Assert
        userRole.AssignedBy.Should().BeNull();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsNull_ReturnsFalse()
    {
        // Arrange
        var userRole = new UserRole(Guid.NewGuid(), Guid.NewGuid(), null)
        {
            ExpiresAt = null
        };

        // Act
        var isExpired = userRole.IsExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsFuture_ReturnsFalse()
    {
        // Arrange
        var userRole = new UserRole(Guid.NewGuid(), Guid.NewGuid(), null)
        {
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var isExpired = userRole.IsExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsPast_ReturnsTrue()
    {
        // Arrange
        var userRole = new UserRole(Guid.NewGuid(), Guid.NewGuid(), null)
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var isExpired = userRole.IsExpired();

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsNow_ReturnsTrue()
    {
        // Arrange
        var userRole = new UserRole(Guid.NewGuid(), Guid.NewGuid(), null)
        {
            ExpiresAt = DateTime.UtcNow
        };

        // Act
        var isExpired = userRole.IsExpired();

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsPermanent_WhenExpiresAtIsNull_ReturnsTrue()
    {
        // Arrange
        var userRole = new UserRole(Guid.NewGuid(), Guid.NewGuid(), null)
        {
            ExpiresAt = null
        };

        // Act
        var isPermanent = userRole.IsPermanent();

        // Assert
        isPermanent.Should().BeTrue();
    }

    [Fact]
    public void IsPermanent_WhenExpiresAtIsSet_ReturnsFalse()
    {
        // Arrange
        var userRole = new UserRole(Guid.NewGuid(), Guid.NewGuid(), null)
        {
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var isPermanent = userRole.IsPermanent();

        // Assert
        isPermanent.Should().BeFalse();
    }

    [Fact]
    public void UserRole_CanSetRole_NavigationProperty()
    {
        // Arrange
        var userRole = new UserRole(Guid.NewGuid(), Guid.NewGuid(), null);
        var role = new Role("TestRole", "Test", null);

        // Act
        userRole.Role = role;

        // Assert
        userRole.Role.Should().NotBeNull();
        userRole.Role.Name.Should().Be("TestRole");
    }
}
