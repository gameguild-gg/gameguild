using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using AuthorizationOptions = GameGuild.Configuration.PresentationLayer.Authorization.AuthorizationOptions;

namespace GameGuild.Identity.Authorization.UnitTests.Security;

/// <summary>
///     Tests for EffectivePermissionResolverService ensuring DENY-WINS semantics.
/// </summary>
public class EffectivePermissionResolverSecurityTests
{
    private readonly Mock<IRbacPermissionResolver> _mockRbacResolver;
    private readonly Mock<ITenantPermissionStore> _mockTenantPermissionStore;
    private readonly Mock<IResourcePermissionStore> _mockResourcePermissionStore;
    private readonly Mock<ILogger<EffectivePermissionResolverService>> _mockLogger;
    private readonly IOptions<AuthorizationOptions> _options;
    private readonly EffectivePermissionResolverService _resolver;

    public EffectivePermissionResolverSecurityTests()
    {
        _mockRbacResolver = new Mock<IRbacPermissionResolver>();
        _mockTenantPermissionStore = new Mock<ITenantPermissionStore>();
        _mockResourcePermissionStore = new Mock<IResourcePermissionStore>();
        _mockLogger = new Mock<ILogger<EffectivePermissionResolverService>>();

        var authOptions = new AuthorizationOptions
        {
            SystemAccountId = Guid.Parse("00000000-0000-0000-0000-000000000001")
        };
        _options = Options.Create(authOptions);

        _resolver = new EffectivePermissionResolverService(
            _mockRbacResolver.Object,
            _mockTenantPermissionStore.Object,
            _mockResourcePermissionStore.Object,
            _options,
            _mockLogger.Object);
    }

    #region DENY-WINS Semantics

    [Fact]
    public async Task ResolveEffectivePermissions_DenyOverridesAllow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // RBAC grants content:read and content:write
        _mockRbacResolver
            .Setup(r => r.GetRolePermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RbacPermissionResult
            {
                Permissions = new HashSet<string> { "content:read", "content:write" },
                DenyPermissions = new HashSet<string>()
            });

        // Tenant permission denies content:write
        var tenantPermission = new TenantPermission
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = Array.Empty<string>(),
            DenyPermissions = new[] { "content:write" }
        };

        _mockTenantPermissionStore
            .Setup(s => s.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantPermission);

        _mockTenantPermissionStore
            .Setup(s => s.GetTenantDefaultsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        _mockTenantPermissionStore
            .Setup(s => s.GetGlobalDefaultsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        // Act
        var result = await _resolver.ResolveEffectivePermissionsAsync(userId, tenantId);

        // Assert - content:write should be denied
        result.Should().Contain("content:read");
        result.Should().NotContain("content:write");
    }

    [Fact]
    public async Task ResolveEffectivePermissions_RoleDenyOverridesRoleAllow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // RBAC grants and denies
        _mockRbacResolver
            .Setup(r => r.GetRolePermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RbacPermissionResult
            {
                Permissions = new HashSet<string> { "admin:*", "content:*" },
                DenyPermissions = new HashSet<string> { "content:delete" }
            });

        _mockTenantPermissionStore
            .Setup(s => s.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        _mockTenantPermissionStore
            .Setup(s => s.GetTenantDefaultsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        _mockTenantPermissionStore
            .Setup(s => s.GetGlobalDefaultsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        // Act
        var result = await _resolver.ResolveEffectivePermissionsAsync(userId, tenantId);

        // Assert
        result.Should().Contain("admin:*");
        result.Should().Contain("content:*");
        result.Should().NotContain("content:delete");
    }

    [Fact]
    public async Task ResolveEffectivePermissions_TenantDenyOverridesGlobalAllow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockRbacResolver
            .Setup(r => r.GetRolePermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RbacPermissionResult());

        // Global default grants content:read
        var globalDefault = new TenantPermission
        {
            UserId = null,
            TenantId = null,
            Permissions = new[] { "content:read" },
            DenyPermissions = Array.Empty<string>()
        };

        // Tenant denies content:read
        var tenantDefault = new TenantPermission
        {
            UserId = null,
            TenantId = tenantId,
            Permissions = Array.Empty<string>(),
            DenyPermissions = new[] { "content:read" }
        };

        _mockTenantPermissionStore
            .Setup(s => s.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        _mockTenantPermissionStore
            .Setup(s => s.GetTenantDefaultsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantDefault);

        _mockTenantPermissionStore
            .Setup(s => s.GetGlobalDefaultsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(globalDefault);

        // Act
        var result = await _resolver.ResolveEffectivePermissionsAsync(userId, tenantId);

        // Assert - global allow should be overridden by tenant deny
        result.Should().NotContain("content:read");
    }

    [Fact]
    public async Task ResolveEffectivePermissions_DirectUserDenyOverridesTenantDefault()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockRbacResolver
            .Setup(r => r.GetRolePermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RbacPermissionResult());

        // Tenant default grants projects:create
        var tenantDefault = new TenantPermission
        {
            UserId = null,
            TenantId = tenantId,
            Permissions = new[] { "projects:create" },
            DenyPermissions = Array.Empty<string>()
        };

        // User-specific deny
        var userPermission = new TenantPermission
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = Array.Empty<string>(),
            DenyPermissions = new[] { "projects:create" }
        };

        _mockTenantPermissionStore
            .Setup(s => s.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermission);

        _mockTenantPermissionStore
            .Setup(s => s.GetTenantDefaultsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantDefault);

        _mockTenantPermissionStore
            .Setup(s => s.GetGlobalDefaultsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        // Act
        var result = await _resolver.ResolveEffectivePermissionsAsync(userId, tenantId);

        // Assert
        result.Should().NotContain("projects:create");
    }

    #endregion

    #region System Account Protection

    [Fact]
    public async Task ResolveEffectivePermissions_SystemAccount_DenyCannotOverrideWildcard()
    {
        // Arrange
        var systemAccountId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var tenantId = Guid.NewGuid();

        _mockRbacResolver
            .Setup(r => r.GetRolePermissionsAsync(systemAccountId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RbacPermissionResult());

        // Attempt to deny system permissions
        var tenantPermission = new TenantPermission
        {
            UserId = systemAccountId,
            TenantId = tenantId,
            Permissions = Array.Empty<string>(),
            DenyPermissions = new[] { "*" } // Attempt to deny everything
        };

        _mockTenantPermissionStore
            .Setup(s => s.GetByUserAndTenantAsync(systemAccountId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantPermission);

        _mockTenantPermissionStore
            .Setup(s => s.GetTenantDefaultsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        _mockTenantPermissionStore
            .Setup(s => s.GetGlobalDefaultsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        // Act
        var result = await _resolver.ResolveEffectivePermissionsAsync(systemAccountId, tenantId);

        // Assert - system account should still have wildcard
        result.Should().Contain("*");
    }

    #endregion

    #region Tenant Isolation

    [Fact]
    public async Task ResolveEffectivePermissions_TenantSpecificPermission_NotInOtherTenants()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        _mockRbacResolver
            .Setup(r => r.GetRolePermissionsAsync(userId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RbacPermissionResult());

        // User has permission in tenant A
        var tenantAPermission = new TenantPermission
        {
            UserId = userId,
            TenantId = tenantA,
            Permissions = new[] { "special:permission" },
            DenyPermissions = Array.Empty<string>()
        };

        _mockTenantPermissionStore
            .Setup(s => s.GetByUserAndTenantAsync(userId, tenantA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantAPermission);

        _mockTenantPermissionStore
            .Setup(s => s.GetByUserAndTenantAsync(userId, tenantB, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        _mockTenantPermissionStore
            .Setup(s => s.GetTenantDefaultsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        _mockTenantPermissionStore
            .Setup(s => s.GetGlobalDefaultsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        // Act
        var resultA = await _resolver.ResolveEffectivePermissionsAsync(userId, tenantA);
        var resultB = await _resolver.ResolveEffectivePermissionsAsync(userId, tenantB);

        // Assert
        resultA.Should().Contain("special:permission");
        resultB.Should().NotContain("special:permission");
    }

    #endregion

    #region Aggregation

    [Fact]
    public async Task ResolveEffectivePermissions_AggregatesAllSources()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // RBAC permissions
        _mockRbacResolver
            .Setup(r => r.GetRolePermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RbacPermissionResult
            {
                Permissions = new HashSet<string> { "role:permission" },
                DenyPermissions = new HashSet<string>()
            });

        // Global default
        var globalDefault = new TenantPermission
        {
            UserId = null,
            TenantId = null,
            Permissions = new[] { "global:permission" },
            DenyPermissions = Array.Empty<string>()
        };

        // Tenant default
        var tenantDefault = new TenantPermission
        {
            UserId = null,
            TenantId = tenantId,
            Permissions = new[] { "tenant:permission" },
            DenyPermissions = Array.Empty<string>()
        };

        // Direct user permission
        var userPermission = new TenantPermission
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = new[] { "user:permission" },
            DenyPermissions = Array.Empty<string>()
        };

        _mockTenantPermissionStore
            .Setup(s => s.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermission);

        _mockTenantPermissionStore
            .Setup(s => s.GetTenantDefaultsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantDefault);

        _mockTenantPermissionStore
            .Setup(s => s.GetGlobalDefaultsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(globalDefault);

        // Act
        var result = await _resolver.ResolveEffectivePermissionsAsync(userId, tenantId);

        // Assert - all sources should be aggregated
        result.Should().Contain("role:permission");
        result.Should().Contain("global:permission");
        result.Should().Contain("tenant:permission");
        result.Should().Contain("user:permission");
    }

    #endregion
}
