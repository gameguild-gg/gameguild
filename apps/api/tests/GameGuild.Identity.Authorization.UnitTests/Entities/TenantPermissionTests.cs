using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Entities;

public class TenantPermissionTests
{
    [Fact]
    public void TenantPermission_DefaultValues_ShouldBeCorrect()
    {
        var tp = new TenantPermission();

        tp.UserId.Should().BeNull();
        tp.TenantId.Should().BeNull();
        tp.Permissions.Should().BeEmpty();
        tp.DenyPermissions.Should().BeEmpty();
        tp.ExpiresAt.Should().BeNull();
        tp.IsActive.Should().BeTrue();
        tp.GrantedBy.Should().BeNull();
        tp.Reason.Should().BeNull();
        tp.Metadata.Should().BeNull();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtNull_ShouldReturnFalse()
    {
        var tp = new TenantPermission { ExpiresAt = null };

        tp.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInFuture_ShouldReturnFalse()
    {
        var tp = new TenantPermission { ExpiresAt = DateTime.UtcNow.AddDays(1) };

        tp.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInPast_ShouldReturnTrue()
    {
        var tp = new TenantPermission { ExpiresAt = DateTime.UtcNow.AddDays(-1) };

        tp.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void HasPermission_WhenPermissionExists_ShouldReturnTrue()
    {
        var tp = new TenantPermission { Permissions = new[] { "Read", "Write" } };

        tp.HasPermission("Read").Should().BeTrue();
    }

    [Fact]
    public void HasPermission_WhenPermissionDoesNotExist_ShouldReturnFalse()
    {
        var tp = new TenantPermission { Permissions = new[] { "Read" } };

        tp.HasPermission("Delete").Should().BeFalse();
    }

    [Fact]
    public void HasPermission_ShouldBeCaseInsensitive()
    {
        var tp = new TenantPermission { Permissions = new[] { "Read" } };

        tp.HasPermission("read").Should().BeTrue();
        tp.HasPermission("READ").Should().BeTrue();
    }

    [Fact]
    public void HasDenyPermission_WhenDenyPermissionExists_ShouldReturnTrue()
    {
        var tp = new TenantPermission { DenyPermissions = new[] { "Delete" } };

        tp.HasDenyPermission("Delete").Should().BeTrue();
    }

    [Fact]
    public void HasDenyPermission_WhenDenyPermissionDoesNotExist_ShouldReturnFalse()
    {
        var tp = new TenantPermission { DenyPermissions = new[] { "Delete" } };

        tp.HasDenyPermission("Read").Should().BeFalse();
    }

    [Fact]
    public void HasDenyPermission_ShouldBeCaseInsensitive()
    {
        var tp = new TenantPermission { DenyPermissions = new[] { "Delete" } };

        tp.HasDenyPermission("delete").Should().BeTrue();
    }

    [Fact]
    public void HasEffectivePermission_WhenAllowedAndNotDenied_ShouldReturnTrue()
    {
        var tp = new TenantPermission
        {
            Permissions = new[] { "Read", "Write" },
            DenyPermissions = new[] { "Delete" }
        };

        tp.HasEffectivePermission("Read").Should().BeTrue();
    }

    [Fact]
    public void HasEffectivePermission_WhenAllowedButDenied_ShouldReturnFalse()
    {
        var tp = new TenantPermission
        {
            Permissions = new[] { "Read", "Write", "Delete" },
            DenyPermissions = new[] { "Delete" }
        };

        tp.HasEffectivePermission("Delete").Should().BeFalse();
    }

    [Fact]
    public void HasEffectivePermission_WhenNotAllowed_ShouldReturnFalse()
    {
        var tp = new TenantPermission
        {
            Permissions = new[] { "Read" },
            DenyPermissions = Array.Empty<string>()
        };

        tp.HasEffectivePermission("Write").Should().BeFalse();
    }

    [Fact]
    public void AddPermissions_ShouldAddNewPermissions()
    {
        var tp = new TenantPermission { Permissions = new[] { "Read" } };

        tp.AddPermissions("Write", "Delete");

        tp.Permissions.Should().Contain("Read");
        tp.Permissions.Should().Contain("Write");
        tp.Permissions.Should().Contain("Delete");
        tp.Permissions.Should().HaveCount(3);
    }

    [Fact]
    public void AddPermissions_ShouldNotAddDuplicates()
    {
        var tp = new TenantPermission { Permissions = new[] { "Read", "Write" } };

        tp.AddPermissions("Read", "Write", "Delete");

        tp.Permissions.Should().HaveCount(3);
    }

    [Fact]
    public void AddPermissions_ShouldBeCaseInsensitiveForDuplicateCheck()
    {
        var tp = new TenantPermission { Permissions = new[] { "Read" } };

        tp.AddPermissions("read");

        tp.Permissions.Should().HaveCount(1);
    }

    [Fact]
    public void RemovePermissions_ShouldRemoveSpecifiedPermissions()
    {
        var tp = new TenantPermission { Permissions = new[] { "Read", "Write", "Delete" } };

        tp.RemovePermissions("Write", "Delete");

        tp.Permissions.Should().HaveCount(1);
        tp.Permissions.Should().Contain("Read");
    }

    [Fact]
    public void RemovePermissions_ShouldBeCaseInsensitive()
    {
        var tp = new TenantPermission { Permissions = new[] { "Read", "Write" } };

        tp.RemovePermissions("read");

        tp.Permissions.Should().HaveCount(1);
        tp.Permissions.Should().Contain("Write");
    }

    [Fact]
    public void RemovePermissions_WhenPermissionNotPresent_ShouldNotThrow()
    {
        var tp = new TenantPermission { Permissions = new[] { "Read" } };

        tp.RemovePermissions("NonExistent");

        tp.Permissions.Should().HaveCount(1);
    }

    [Fact]
    public void AddDenyPermissions_ShouldAddNewDenyPermissions()
    {
        var tp = new TenantPermission { DenyPermissions = new[] { "Delete" } };

        tp.AddDenyPermissions("Write", "Admin");

        tp.DenyPermissions.Should().HaveCount(3);
        tp.DenyPermissions.Should().Contain("Delete");
        tp.DenyPermissions.Should().Contain("Write");
        tp.DenyPermissions.Should().Contain("Admin");
    }

    [Fact]
    public void AddDenyPermissions_ShouldNotAddDuplicates()
    {
        var tp = new TenantPermission { DenyPermissions = new[] { "Delete" } };

        tp.AddDenyPermissions("Delete", "Write");

        tp.DenyPermissions.Should().HaveCount(2);
    }

    [Fact]
    public void AddDenyPermissions_ShouldBeCaseInsensitiveForDuplicateCheck()
    {
        var tp = new TenantPermission { DenyPermissions = new[] { "Delete" } };

        tp.AddDenyPermissions("delete");

        tp.DenyPermissions.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveDenyPermissions_ShouldRemoveSpecified()
    {
        var tp = new TenantPermission { DenyPermissions = new[] { "Delete", "Admin", "Write" } };

        tp.RemoveDenyPermissions("Admin", "Write");

        tp.DenyPermissions.Should().HaveCount(1);
        tp.DenyPermissions.Should().Contain("Delete");
    }

    [Fact]
    public void RemoveDenyPermissions_ShouldBeCaseInsensitive()
    {
        var tp = new TenantPermission { DenyPermissions = new[] { "Delete", "Admin" } };

        tp.RemoveDenyPermissions("delete");

        tp.DenyPermissions.Should().HaveCount(1);
        tp.DenyPermissions.Should().Contain("Admin");
    }

    [Fact]
    public void Expire_ShouldSetExpiresAtAndDeactivate()
    {
        var tp = new TenantPermission
        {
            IsActive = true,
            ExpiresAt = null
        };

        tp.Expire();

        tp.ExpiresAt.Should().NotBeNull();
        tp.IsActive.Should().BeFalse();
    }
}

public class PermissionTemplateTests
{
    [Fact]
    public void PermissionTemplate_DefaultValues_ShouldBeCorrect()
    {
        var template = new PermissionTemplate();

        template.Name.Should().BeNull();
        template.Description.Should().BeNull();
        template.Permissions.Should().BeEmpty();
        template.IsSystemTemplate.Should().BeFalse();
        template.IsActive.Should().BeTrue();
        template.Category.Should().BeNull();
        template.MinimumTier.Should().BeNull();
        template.Metadata.Should().BeNull();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var template = new PermissionTemplate { IsActive = false };

        template.Activate();

        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var template = new PermissionTemplate { IsActive = true };

        template.Deactivate();

        template.IsActive.Should().BeFalse();
    }

    [Fact]
    public void PermissionTemplate_ShouldStoreAllProperties()
    {
        var template = new PermissionTemplate
        {
            Name = "Admin Template",
            Description = "Full admin access",
            Permissions = new[] { "Read", "Write", "Delete", "Admin" },
            IsSystemTemplate = true,
            Category = "Administration",
            MinimumTier = "Enterprise",
            Metadata = new Dictionary<string, object> { { "version", "2.0" } }
        };

        template.Name.Should().Be("Admin Template");
        template.Description.Should().Be("Full admin access");
        template.Permissions.Should().HaveCount(4);
        template.IsSystemTemplate.Should().BeTrue();
        template.Category.Should().Be("Administration");
        template.MinimumTier.Should().Be("Enterprise");
        template.Metadata.Should().ContainKey("version");
    }
}
