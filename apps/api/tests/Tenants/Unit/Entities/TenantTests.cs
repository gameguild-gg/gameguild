using FluentAssertions;
using GameGuild.Modules.Tenants;
using Xunit;

namespace GameGuild.Tests.Tenants.Unit.Entities;

/// <summary>
/// Unit tests for Tenant entity
/// </summary>
public class TenantTests
{
    [Fact]
    public void Constructor_Should_Create_Tenant_With_Default_Values()
    {
        // Act
        var tenant = new Tenant();

        // Assert  
        _ = tenant.Name.Should().Be(string.Empty);
        _ = tenant.Description.Should().BeNull();
        _ = tenant.IsActive.Should().BeTrue();
        _ = tenant.IsDefault.Should().BeFalse();
        _ = tenant.Slug.Should().Be(string.Empty);
        _ = tenant.Id.Should().NotBeEmpty();
        _ = tenant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _ = tenant.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Activate_Should_Set_IsActive_To_True_And_Update_Timestamp()
    {
        // Arrange
        var tenant = new Tenant { IsActive = false };
        DateTime originalUpdatedAt = tenant.UpdatedAt;

        // Act
        tenant.Activate();

        // Assert
        _ = tenant.IsActive.Should().BeTrue();
        _ = tenant.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_To_False_And_Update_Timestamp()
    {
        // Arrange
        var tenant = new Tenant { IsActive = true };
        DateTime originalUpdatedAt = tenant.UpdatedAt;

        // Act
        tenant.Deactivate();

        // Assert
        _ = tenant.IsActive.Should().BeFalse();
        _ = tenant.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Update_Should_Set_Name_And_Description_And_Update_Timestamp()
    {
        // Arrange
        var tenant = new Tenant();
        DateTime originalUpdatedAt = tenant.UpdatedAt;
        const string newName = "Updated Tenant";
        const string newDescription = "Updated Description";

        // Act
        tenant.Update(newName, newDescription);

        // Assert
        _ = tenant.Name.Should().Be(newName);
        _ = tenant.Description.Should().Be(newDescription);
        _ = tenant.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Update_Should_Set_Name_And_Null_Description_When_Description_Not_Provided()
    {
        // Arrange
        var tenant = new Tenant { Description = "Old Description" };
        const string newName = "Updated Tenant";

        // Act
        tenant.Update(newName);

        // Assert
        _ = tenant.Name.Should().Be(newName);
        _ = tenant.Description.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Update_Should_Accept_Empty_Or_Null_Name(string? name)
    {
        // Arrange
        var tenant = new Tenant();

        // Act & Assert - Should not throw
        tenant.Update(name!);
        _ = tenant.Name.Should().Be(name); // Keep original value (null stays null, empty stays empty)
    }

    [Fact]
    public void Tenant_Should_Inherit_From_EntityBase()
    {
        // Arrange & Act
        var tenant = new Tenant();

        // Assert
        _ = tenant.Should().BeAssignableTo<EntityBase>();
    }

    [Fact]
    public void Tenant_Should_Have_Correct_Properties()
    {
        // Act
        var tenant = new Tenant
        {
            Name = "Test Tenant",
            Description = "Test Description",
            IsActive = false,
            IsDefault = true,
            Slug = "test-tenant"
        };

        // Assert
        _ = tenant.Name.Should().Be("Test Tenant");
        _ = tenant.Description.Should().Be("Test Description");
        _ = tenant.IsActive.Should().BeFalse();
        _ = tenant.IsDefault.Should().BeTrue();
        _ = tenant.Slug.Should().Be("test-tenant");
    }
}