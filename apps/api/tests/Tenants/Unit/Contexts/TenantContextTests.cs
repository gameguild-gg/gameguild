using FluentAssertions;
using GameGuild.Modules.Tenants;
using Xunit;

namespace GameGuild.Tests.Tenants.Unit.Contexts;

/// <summary>
/// Unit tests for TenantContext
/// </summary>
public class TenantContextTests
{
    [Fact]
    public void Constructor_Should_Initialize_With_Null_CurrentTenant()
    {
        // Act
        var context = new TenantContext();

        // Assert
        _ = context.CurrentTenant.Should().BeNull();
        _ = context.CurrentTenantId.Should().BeNull();
    }

    [Fact]
    public void SetCurrentTenant_Should_Set_CurrentTenant()
    {
        // Arrange
        var context = new TenantContext();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Slug = "test-tenant"
        };

        // Act
        context.SetCurrentTenant(tenant);

        // Assert
        _ = context.CurrentTenant.Should().BeEquivalentTo(tenant);
        _ = context.CurrentTenantId.Should().Be(tenant.Id);
    }

    [Fact]
    public void SetCurrentTenant_Should_Accept_Null_Tenant()
    {
        // Arrange
        var context = new TenantContext();
        var tenant = new Tenant { Id = Guid.NewGuid() };
        context.SetCurrentTenant(tenant);

        // Act
        context.SetCurrentTenant(null);

        // Assert
        _ = context.CurrentTenant.Should().BeNull();
        _ = context.CurrentTenantId.Should().BeNull();
    }

    [Fact]
    public void CurrentTenantId_Should_Return_Tenant_Id_When_Tenant_Set()
    {
        // Arrange
        var context = new TenantContext();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId };

        // Act
        context.SetCurrentTenant(tenant);

        // Assert
        _ = context.CurrentTenantId.Should().Be(tenantId);
    }

    [Fact]
    public void CurrentTenantId_Should_Return_Null_When_No_Tenant_Set()
    {
        // Arrange
        var context = new TenantContext();

        // Assert
        _ = context.CurrentTenantId.Should().BeNull();
    }

    [Fact]
    public void TenantContext_Should_Implement_ITenantContext()
    {
        // Arrange & Act
        var context = new TenantContext();

        // Assert
        _ = context.Should().BeAssignableTo<ITenantContext>();
    }

    [Fact]
    public void SetCurrentTenant_Should_Allow_Multiple_Changes()
    {
        // Arrange
        var context = new TenantContext();
        var tenant1 = new Tenant { Id = Guid.NewGuid(), Name = "Tenant 1" };
        var tenant2 = new Tenant { Id = Guid.NewGuid(), Name = "Tenant 2" };

        // Act & Assert
        context.SetCurrentTenant(tenant1);
        _ = context.CurrentTenant.Should().BeEquivalentTo(tenant1);
        _ = context.CurrentTenantId.Should().Be(tenant1.Id);

        context.SetCurrentTenant(tenant2);
        _ = context.CurrentTenant.Should().BeEquivalentTo(tenant2);
        _ = context.CurrentTenantId.Should().Be(tenant2.Id);

        context.SetCurrentTenant(null);
        _ = context.CurrentTenant.Should().BeNull();
        _ = context.CurrentTenantId.Should().BeNull();
    }
}