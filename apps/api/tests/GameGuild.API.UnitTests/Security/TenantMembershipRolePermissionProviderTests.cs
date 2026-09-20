using FluentAssertions;
using TenantMembershipRolePermissionProvider = GameGuild.API.Security.TenantMembershipRolePermissionProvider;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Tenants;
using Moq;
using Xunit;

namespace GameGuild.API.UnitTests.Security;

/// <summary>
///     Tests for the composition-root provider that projects this product's built-in
///     tenant membership roles into the dynamic permission pipeline.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Security", "TenantAuthorization")]
public sealed class TenantMembershipRolePermissionProviderTests
{
    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    [InlineData("TenantAdmin")]
    public async Task GetPermissionsAsync_ShouldGrantTenantAdminPermission_ForAdministrativeMembership(string role)
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = new Mock<ITenantMemberRepository>();
        repository
            .Setup(candidate => candidate.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantMember
            {
                UserId = userId,
                TenantId = tenantId,
                Role = role,
                IsActive = true
            });

        var permissions = await new TenantMembershipRolePermissionProvider(repository.Object)
            .GetPermissionsAsync(userId, tenantId);

        permissions.Should().Contain(AdminPermission.Keys.TenantAdmin);
        permissions.Should().Contain(UsersPermission.Keys.Manage);
    }

    [Fact]
    public async Task GetPermissionsAsync_ShouldNotGrantTenantAdminPermission_ForNonAdministrativeMembership()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = new Mock<ITenantMemberRepository>();
        repository
            .Setup(candidate => candidate.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantMember
            {
                UserId = userId,
                TenantId = tenantId,
                Role = TenantRole.Member,
                IsActive = true
            });

        var permissions = await new TenantMembershipRolePermissionProvider(repository.Object)
            .GetPermissionsAsync(userId, tenantId);

        permissions.Should().Contain([
            UsersPermission.Keys.ReadSelf,
            UsersPermission.Keys.EditSelf,
            UsersPermission.Keys.DeleteSelf
        ]);
        permissions.Should().NotContain([
            AdminPermission.Keys.TenantAdmin,
            UsersPermission.Keys.Manage
        ]);
    }

    [Fact]
    public async Task GetPermissionsAsync_ShouldGrantNoPermissions_ForInactiveMembership()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = new Mock<ITenantMemberRepository>();
        repository
            .Setup(candidate => candidate.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantMember
            {
                UserId = userId,
                TenantId = tenantId,
                Role = TenantRole.Owner,
                IsActive = false
            });

        var permissions = await new TenantMembershipRolePermissionProvider(repository.Object)
            .GetPermissionsAsync(userId, tenantId);

        permissions.Should().BeEmpty();
    }
}
