using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Entities;

/// <summary>
///     Regression coverage for the soft-delete fail-closed defect (#352 audit): a grant
///     whose entity was soft-deleted must never report effective or convey permissions —
///     even while its stored IsActive flag still reads true. Mirrors the audit's exact
///     reproduction: SoftDelete() stamps DeletedAt without deactivating.
/// </summary>
public sealed class SoftDeletedPermissionEffectivenessTests
{
    private static TenantPermission DeletedActiveGrant()
    {
        var grant = new TenantPermission
        {
            Permissions = ["Read"],
            DenyPermissions = [],
        };
        // Simulate a persisted grant: SoftDelete requires a non-zero version.
        typeof(PermissionBase)
            .GetProperty(nameof(PermissionBase.Version))!
            .SetValue(grant, 1);
        grant.SoftDelete();
        return grant;
    }

    [Fact]
    public void SoftDeleted_Grant_Is_Never_Effective()
    {
        var grant = DeletedActiveGrant();

        Assert.True(grant.IsDeleted);
        // The stored flag stays true until a save reconciles it; effectiveness must not care.
        Assert.True(grant.IsActive);
        Assert.False(grant.IsEffective());
    }

    [Fact]
    public void SoftDeleted_Grant_Never_Conveys_Permissions()
    {
        var grant = DeletedActiveGrant();

        Assert.False(grant.Grants(PermissionType.Read));
    }

    [Fact]
    public void SoftDeleted_ResourceUserPermission_Is_Never_Active()
    {
        var grant = new ResourceUserPermission
        {
            UserId = Guid.NewGuid(),
            TenantId = new TenantId(Guid.NewGuid()),
            ResourceId = Guid.NewGuid().ToString(),
            ResourceType = "document",
            GrantedByUserId = Guid.NewGuid(),
            Permissions = ["Read"],
        };
        typeof(PermissionBase)
            .GetProperty(nameof(PermissionBase.Version))!
            .SetValue(grant, 1);
        grant.SoftDelete();

        Assert.True(grant.IsDeleted);
        Assert.False(grant.IsActive);
    }

    [Fact]
    public void Live_Grant_Remains_Effective()
    {
        var grant = new TenantPermission
        {
            Permissions = ["Read"],
            DenyPermissions = [],
        };

        Assert.False(grant.IsDeleted);
        Assert.True(grant.IsEffective());
        Assert.True(grant.Grants(PermissionType.Read));
    }
}
