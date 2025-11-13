using FluentAssertions;
using GameGuild.Tenants.Entities;
using Xunit;

namespace GameGuild.Tests.Tenants.Unit.Entities;

/// <summary>
/// Unit tests for Tenant entity
/// </summary>
public class TenantTests
{
    [Fact]
    public void Tenant_Should_Be_Created_With_Valid_Properties()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Slug = "test-tenant",
            AdminEmail = "admin@test.com",
            IsActive = true
        };

        // Assert
        tenant.Should().NotBeNull();
        tenant.Name.Should().Be("Test Tenant");
        tenant.Slug.Should().Be("test-tenant");
        tenant.AdminEmail.Should().Be("admin@test.com");
        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Tenant_Should_Have_Default_IsDefault_As_False()
    {
        // Arrange & Act
        var tenant = new Tenant();

        // Assert
        tenant.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Tenant_Should_Support_Activation()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Slug = "test",
            AdminEmail = "admin@test.com",
            IsActive = false
        };

        // Act
        tenant.Activate();

        // Assert
        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Tenant_Should_Support_Deactivation()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Slug = "test",
            AdminEmail = "admin@test.com",
            IsActive = true
        };

        // Act
        tenant.Deactivate();

        // Assert
        tenant.IsActive.Should().BeFalse();
    }
}
