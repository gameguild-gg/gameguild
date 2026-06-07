using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Entities;

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
    public void Tenant_Partial_Constructor_Should_Map_Properties()
    {
        var tenant = new Tenant(new { Name = "Partial Tenant", Slug = "partial-tenant", IsActive = true });

        tenant.Name.Should().Be("Partial Tenant");
        tenant.Slug.Should().Be("partial-tenant");
        tenant.IsActive.Should().BeTrue();
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

    [Fact]
    public void Activate_Should_Add_Domain_Event()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.ClearDomainEvents();

        // Act
        tenant.Activate();

        // Assert
        tenant.DomainEvents.Should().ContainSingle(e => e is TenantActivatedEvent);
    }

    [Fact]
    public void Deactivate_Should_Add_Domain_Event()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.ClearDomainEvents();

        // Act
        tenant.Deactivate();

        // Assert
        tenant.DomainEvents.Should().ContainSingle(e => e is TenantDeactivatedEvent);
    }

    [Fact]
    public void Update_Should_Change_Name_And_Description_And_Add_Event()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.ClearDomainEvents();

        // Act
        tenant.Update("Updated Name", "Updated description");

        // Assert
        tenant.Name.Should().Be("Updated Name");
        tenant.Description.Should().Be("Updated description");
        tenant.DomainEvents.Should().ContainSingle(e => e is TenantUpdatedEvent);
    }

    [Fact]
    public void Archive_Should_Mark_As_Archived_And_Inactive()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.ClearDomainEvents();

        // Act
        tenant.Archive("compliance");

        // Assert
        tenant.IsArchived.Should().BeTrue();
        tenant.ArchivedAt.Should().NotBeNull();
        tenant.IsActive.Should().BeFalse();
        tenant.DomainEvents.Should().ContainSingle(e => e is TenantArchivedEvent);
    }

    [Fact]
    public void Unarchive_Should_Restore_Tenant()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.Archive("test");
        tenant.ClearDomainEvents();

        // Act
        tenant.Unarchive();

        // Assert
        tenant.IsArchived.Should().BeFalse();
        tenant.ArchivedAt.Should().BeNull();
        tenant.IsActive.Should().BeTrue();
        tenant.DomainEvents.Should().ContainSingle(e => e is TenantRestoredEvent);
    }

    [Fact]
    public void ValidateForMemberAddition_Should_Fail_When_Inactive()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.IsActive = false;

        // Act
        var result = tenant.ValidateForMemberAddition(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("inactive");
    }

    [Fact]
    public void ValidateForMemberAddition_Should_Fail_When_Archived()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.IsArchived = true;

        // Act
        var result = tenant.ValidateForMemberAddition(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("archived");
    }

    [Fact]
    public void ValidateForMemberAddition_Should_Fail_When_Already_Member()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        var userId = Guid.NewGuid();
        tenant.TenantMembers.Add(new TenantMember { TenantId = tenant.Id, UserId = userId, IsActive = true, Role = "Member" });

        // Act
        var result = tenant.ValidateForMemberAddition(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already a member");
    }

    [Fact]
    public void ValidateForMemberAddition_Should_Succeed_When_Eligible()
    {
        // Arrange
        var tenant = CreateActiveTenant();

        // Act
        var result = tenant.ValidateForMemberAddition(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ValidateConfiguration_Should_Return_Errors_For_Invalid_Fields()
    {
        // Arrange
        var tenant = new Tenant
        {
            Name = "",
            Slug = "INVALID SLUG"
        };

        // Act
        var result = tenant.ValidateConfiguration();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Tenant name is required.");
        result.Errors.Should().Contain("Tenant slug must contain only lowercase letters, numbers, and hyphens.");
    }

    [Fact]
    public void ValidateConfiguration_Should_Return_Error_For_Required_Slug()
    {
        var tenant = new Tenant
        {
            Name = "Valid Name",
            Slug = string.Empty
        };

        var result = tenant.ValidateConfiguration();

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Tenant slug is required.");
        result.Errors.Should().Contain("Tenant slug must contain only lowercase letters, numbers, and hyphens.");
    }

    [Fact]
    public void ValidateConfiguration_Should_Succeed_For_Valid_Tenant()
    {
        // Arrange
        var tenant = new Tenant { Name = "Valid", Slug = "valid-tenant" };

        // Act
        var result = tenant.ValidateConfiguration();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ActiveMemberCount_Should_Count_Active_Members()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.TenantMembers.Add(new TenantMember { TenantId = tenant.Id, UserId = Guid.NewGuid(), IsActive = true, Role = "Member" });
        tenant.TenantMembers.Add(new TenantMember { TenantId = tenant.Id, UserId = Guid.NewGuid(), IsActive = false, Role = "Member" });

        // Act
        var count = tenant.ActiveMemberCount;

        // Assert
        count.Should().Be(1);
        tenant.HasActiveMembers.Should().BeTrue();
    }

    [Fact]
    public void CanAcceptMembers_Should_Require_Active_And_Not_Archived()
    {
        var tenant = CreateActiveTenant();

        tenant.CanAcceptMembers.Should().BeTrue();

        tenant.IsArchived = true;
        tenant.CanAcceptMembers.Should().BeFalse();

        tenant.IsArchived = false;
        tenant.IsActive = false;
        tenant.CanAcceptMembers.Should().BeFalse();
    }

    [Fact]
    public void ValidateForArchive_Should_Return_Success_With_Active_Member_Count()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.TenantMembers.Add(new TenantMember { TenantId = tenant.Id, UserId = Guid.NewGuid(), IsActive = true, Role = "Member" });

        // Act
        var result = tenant.ValidateForArchive();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AffectedMemberCount.Should().Be(1);
    }

    [Fact]
    public void ValidateForArchive_Should_Fail_When_Already_Archived()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.IsArchived = true;

        // Act
        var result = tenant.ValidateForArchive();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already archived");
    }

    [Fact]
    public void ValidateForDeletion_Should_Fail_When_Not_Archived()
    {
        // Arrange
        var tenant = CreateActiveTenant();

        // Act
        var result = tenant.ValidateForDeletion();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("must be archived");
    }

    [Fact]
    public void ValidateForDeletion_Should_Fail_When_Has_Active_Members()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.IsArchived = true;
        tenant.TenantMembers.Add(new TenantMember { TenantId = tenant.Id, UserId = Guid.NewGuid(), IsActive = true, Role = "Member" });

        // Act
        var result = tenant.ValidateForDeletion();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("active members");
    }

    [Fact]
    public void ValidateForDeletion_Should_Succeed_When_Archived_And_No_Active_Members()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.IsArchived = true;

        // Act
        var result = tenant.ValidateForDeletion();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    private static Tenant CreateActiveTenant()
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Slug = "test-tenant",
            AdminEmail = "admin@test.com",
            IsActive = true
        };
    }
}
