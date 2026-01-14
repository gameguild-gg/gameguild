using System.Security.Claims;
using FluentAssertions;
using GameGuild.Abstractions;
using GameGuild.Identity.Permissions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using GameGuild.API;

namespace GameGuild.Identity.Authorization.IntegrationTests;

/// <summary>
/// Integration tests for permission resolution (RBAC + ABAC + DENY-WINS)
/// </summary>
public class PermissionResolutionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PermissionResolutionIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RBAC_RolePermission_IsGranted()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Create role with permission
        var role = new RoleEntity
        {
            Id = roleId,
            Name = "ContentEditor",
            TenantId = tenantId,
            IsActive = true
        };

        var permission = new RolePermissionEntity
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionName = "content.edit",
            Effect = PermissionEffect.Allow
        };

        dbContext.Set<RoleEntity>().Add(role);
        dbContext.Set<RolePermissionEntity>().Add(permission);
        await dbContext.SaveChangesAsync();

        var permissionResolver = scope.ServiceProvider.GetRequiredService<IPermissionResolver>();

        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString()),
            new(ClaimNames.Role, roleId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var hasPermission = await permissionResolver.HasPermissionAsync(user, "content.edit");

        // Assert
        hasPermission.Should().BeTrue();
    }

    [Fact]
    public async Task ABAC_UserPermission_IsGranted()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Create direct user permission
        var permission = new UserPermissionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PermissionName = "admin.access",
            Effect = PermissionEffect.Allow,
            ResourceId = null, // Global permission
            TenantId = tenantId
        };

        dbContext.Set<UserPermissionEntity>().Add(permission);
        await dbContext.SaveChangesAsync();

        var permissionResolver = scope.ServiceProvider.GetRequiredService<IPermissionResolver>();

        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var hasPermission = await permissionResolver.HasPermissionAsync(user, "admin.access");

        // Assert
        hasPermission.Should().BeTrue();
    }

    [Fact]
    public async Task DENY_WINS_UserDenyOverridesRoleAllow()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Role grants permission
        var role = new RoleEntity
        {
            Id = roleId,
            Name = "Editor",
            TenantId = tenantId,
            IsActive = true
        };

        var rolePermission = new RolePermissionEntity
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionName = "content.delete",
            Effect = PermissionEffect.Allow
        };

        // User explicitly denied
        var userPermission = new UserPermissionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PermissionName = "content.delete",
            Effect = PermissionEffect.Deny,
            ResourceId = null,
            TenantId = tenantId
        };

        dbContext.Set<RoleEntity>().Add(role);
        dbContext.Set<RolePermissionEntity>().Add(rolePermission);
        dbContext.Set<UserPermissionEntity>().Add(userPermission);
        await dbContext.SaveChangesAsync();

        var permissionResolver = scope.ServiceProvider.GetRequiredService<IPermissionResolver>();

        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString()),
            new(ClaimNames.Role, roleId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var hasPermission = await permissionResolver.HasPermissionAsync(user, "content.delete");

        // Assert - DENY wins
        hasPermission.Should().BeFalse();
    }

    [Fact]
    public async Task DENY_WINS_RoleDenyOverridesUserAllow()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Role denies permission
        var role = new RoleEntity
        {
            Id = roleId,
            Name = "RestrictedRole",
            TenantId = tenantId,
            IsActive = true
        };

        var rolePermission = new RolePermissionEntity
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionName = "billing.modify",
            Effect = PermissionEffect.Deny
        };

        // User explicitly allowed
        var userPermission = new UserPermissionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PermissionName = "billing.modify",
            Effect = PermissionEffect.Allow,
            ResourceId = null,
            TenantId = tenantId
        };

        dbContext.Set<RoleEntity>().Add(role);
        dbContext.Set<RolePermissionEntity>().Add(rolePermission);
        dbContext.Set<UserPermissionEntity>().Add(userPermission);
        await dbContext.SaveChangesAsync();

        var permissionResolver = scope.ServiceProvider.GetRequiredService<IPermissionResolver>();

        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString()),
            new(ClaimNames.Role, roleId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var hasPermission = await permissionResolver.HasPermissionAsync(user, "billing.modify");

        // Assert - DENY wins
        hasPermission.Should().BeFalse();
    }

    [Fact]
    public async Task ResourceSpecific_Permission_IsGranted()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Create resource-specific permission
        var permission = new UserPermissionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PermissionName = "project.edit",
            Effect = PermissionEffect.Allow,
            ResourceId = resourceId.ToString(), // Specific resource
            TenantId = tenantId
        };

        dbContext.Set<UserPermissionEntity>().Add(permission);
        await dbContext.SaveChangesAsync();

        var permissionResolver = scope.ServiceProvider.GetRequiredService<IPermissionResolver>();

        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act - Check permission for specific resource
        var hasPermission = await permissionResolver.HasPermissionAsync(user, "project.edit", resourceId.ToString());

        // Assert
        hasPermission.Should().BeTrue();
    }

    [Fact]
    public async Task ResourceSpecific_Permission_DeniedForDifferentResource()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var allowedResourceId = Guid.NewGuid();
        var deniedResourceId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Create resource-specific permission
        var permission = new UserPermissionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PermissionName = "project.edit",
            Effect = PermissionEffect.Allow,
            ResourceId = allowedResourceId.ToString(), // Only this resource
            TenantId = tenantId
        };

        dbContext.Set<UserPermissionEntity>().Add(permission);
        await dbContext.SaveChangesAsync();

        var permissionResolver = scope.ServiceProvider.GetRequiredService<IPermissionResolver>();

        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act - Check permission for different resource
        var hasPermission = await permissionResolver.HasPermissionAsync(user, "project.edit", deniedResourceId.ToString());

        // Assert - Should not have permission for different resource
        hasPermission.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleRoles_PermissionsAreAggregated()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var role1Id = Guid.NewGuid();
        var role2Id = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Create two roles with different permissions
        var role1 = new RoleEntity { Id = role1Id, Name = "Role1", TenantId = tenantId, IsActive = true };
        var role2 = new RoleEntity { Id = role2Id, Name = "Role2", TenantId = tenantId, IsActive = true };

        var permission1 = new RolePermissionEntity
        {
            Id = Guid.NewGuid(),
            RoleId = role1Id,
            PermissionName = "feature.alpha",
            Effect = PermissionEffect.Allow
        };

        var permission2 = new RolePermissionEntity
        {
            Id = Guid.NewGuid(),
            RoleId = role2Id,
            PermissionName = "feature.beta",
            Effect = PermissionEffect.Allow
        };

        dbContext.Set<RoleEntity>().AddRange(role1, role2);
        dbContext.Set<RolePermissionEntity>().AddRange(permission1, permission2);
        await dbContext.SaveChangesAsync();

        var permissionResolver = scope.ServiceProvider.GetRequiredService<IPermissionResolver>();

        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString()),
            new(ClaimNames.Role, role1Id.ToString()),
            new(ClaimNames.Role, role2Id.ToString()) // Multiple roles
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var hasAlpha = await permissionResolver.HasPermissionAsync(user, "feature.alpha");
        var hasBeta = await permissionResolver.HasPermissionAsync(user, "feature.beta");

        // Assert - Should have both permissions from different roles
        hasAlpha.Should().BeTrue();
        hasBeta.Should().BeTrue();
    }
}
