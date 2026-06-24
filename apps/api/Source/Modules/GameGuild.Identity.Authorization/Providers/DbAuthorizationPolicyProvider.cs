using System.Security.Claims;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MsAuthorizationOptions = Microsoft.AspNetCore.Authorization.AuthorizationOptions;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Dynamic authorization policy provider that loads policies from the database
///     with tenant-aware caching and version-based invalidation.
///     Registered as Singleton to satisfy ASP.NET Core requirements.
///     Uses IServiceScopeFactory to resolve scoped services (DbContext, repositories).
/// </summary>
public sealed class DbAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly MsAuthorizationOptions _authzOptions;
    private readonly IPolicyCache _policyCache;
    private readonly IPolicyMerger _policyMerger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TenancyOptions _tenancyOptions;
    private readonly ILogger<DbAuthorizationPolicyProvider> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="DbAuthorizationPolicyProvider"/>.
    /// </summary>
    public DbAuthorizationPolicyProvider(
        IOptions<MsAuthorizationOptions> authzOptions,
        IPolicyCache policyCache,
        IPolicyMerger policyMerger,
        IServiceScopeFactory scopeFactory,
        IOptions<TenancyOptions> tenancyOptions,
        ILogger<DbAuthorizationPolicyProvider> logger)
    {
        _authzOptions = authzOptions.Value;
        _policyCache = policyCache;
        _policyMerger = policyMerger;
        _scopeFactory = scopeFactory;
        _tenancyOptions = tenancyOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check built-in policies first
        var builtInPolicy = _authzOptions.GetPolicy(policyName);
        if (builtInPolicy is not null)
            return builtInPolicy;

        // Resolve tenant context and scoped services within a scope
        string tenantId;
        long version;
        PolicyDefinition? baseDefinition;
        PolicyDefinition? tenantDefinition;

        using (var scope = _scopeFactory.CreateScope())
        {
            var tenantContext = scope.ServiceProvider.GetService<IAuthorizationTenantContext>();
            // TenantId is now Guid? - convert to string for cache key, or use default
            tenantId = tenantContext?.TenantId?.ToString() ?? _tenancyOptions.DefaultTenantId;

            var versionStore = scope.ServiceProvider.GetRequiredService<ITenantSecurityVersionStore>();
            version = await versionStore.GetVersionAsync(tenantId).ConfigureAwait(false);

            // Check cache first (before loading from database)
            var cachedPolicy = _policyCache.Get(policyName, tenantId, version);
            if (cachedPolicy is not null)
            {
                _logger.LogDebug("Policy '{PolicyName}' loaded from cache for tenant '{TenantId}'",
                    policyName, tenantId);
                return cachedPolicy;
            }

            // Load from store (scoped service)
            var policyStore = scope.ServiceProvider.GetRequiredService<IPolicyDefinitionStore>();
            
            baseDefinition = await policyStore.GetPolicyAsync(
                policyName,
                tenantId: null).ConfigureAwait(false);

            if (baseDefinition is null)
            {
                var fallbackPolicy = TryBuildStaticFallbackPolicy(policyName);
                if (fallbackPolicy is not null)
                {
                    _policyCache.Set(policyName, tenantId, version, fallbackPolicy);
                    _logger.LogWarning(
                        "Policy '{PolicyName}' not found in the policy store. Using static registered-policy fallback for tenant '{TenantId}'.",
                        policyName,
                        tenantId);
                    return fallbackPolicy;
                }

                _logger.LogDebug("Policy '{PolicyName}' not found", policyName);
                return null;
            }

            tenantDefinition = !string.Equals(tenantId, _tenancyOptions.DefaultTenantId, StringComparison.Ordinal)
                ? await policyStore.GetPolicyAsync(policyName, tenantId).ConfigureAwait(false)
                : null;
        }

        // Merge and build (can be done outside the scope)
        var mergedDefinition = _policyMerger.Merge(baseDefinition, tenantDefinition);
        var policy = _policyMerger.Build(mergedDefinition);

        // Cache the compiled policy
        _policyCache.Set(policyName, tenantId, version, policy);

        _logger.LogDebug("Policy '{PolicyName}' compiled and cached for tenant '{TenantId}'",
            policyName, tenantId);

        return policy;
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return Task.FromResult(_authzOptions.DefaultPolicy);
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return Task.FromResult(_authzOptions.FallbackPolicy);
    }

    private static AuthorizationPolicy? TryBuildStaticFallbackPolicy(string policyName)
    {
        if (!Policies.IsValid(policyName))
            return null;

        if (string.Equals(policyName, Policies.Anonymous, StringComparison.Ordinal))
            return new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();

        var builder = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();

        if (RequiresTenantMatch(policyName))
            builder.AddRequirements(new TenantMatchRequirement());

        var requiredPermission = MapPolicyToPermission(policyName);
        if (requiredPermission is not null)
        {
            builder.RequireAssertion(context =>
                HasAdminRole(context.User) ||
                HasPermission(context.User, requiredPermission));
        }
        else if (string.Equals(policyName, Policies.Admin, StringComparison.Ordinal))
        {
            builder.RequireAssertion(context => HasAdminRole(context.User));
        }
        else if (string.Equals(policyName, Policies.SecureAdmin, StringComparison.Ordinal))
        {
            builder.RequireAssertion(context =>
                HasAdminRole(context.User) &&
                (ClaimsExtractor.IsMfaVerified(context.User) || HasAuthenticationMethod(context.User, "mfa")));
        }
        else if (string.Equals(policyName, Policies.TenantAdmin, StringComparison.Ordinal))
        {
            builder.RequireAssertion(context =>
                HasAdminRole(context.User) ||
                HasRole(context.User, "Owner") ||
                HasRole(context.User, "TenantAdmin") ||
                HasPermission(context.User, "tenant:admin"));
        }

        return builder.Build();
    }

    private static bool RequiresTenantMatch(string policyName)
    {
        if (string.Equals(policyName, Policies.TenantMember, StringComparison.Ordinal) ||
            string.Equals(policyName, Policies.TenantAdmin, StringComparison.Ordinal) ||
            string.Equals(policyName, Policies.SecureAdmin, StringComparison.Ordinal))
            return true;

        return policyName.Contains('.', StringComparison.Ordinal) &&
               !string.Equals(policyName, Policies.Admin, StringComparison.Ordinal);
    }

    private static string? MapPolicyToPermission(string policyName) => policyName switch
    {
        Policies.UsersRead => "users:read",
        Policies.UsersCreate => "users:create",
        Policies.UsersUpdate => "users:update",
        Policies.UsersDelete => "users:delete",
        Policies.UsersAdmin => "users:admin",
        Policies.UsersPurge => "users:purge",
        Policies.UsersReadSelf => "users:read:self",
        Policies.UsersEditSelf => "users:edit:self",
        Policies.UsersDeleteSelf => "users:delete:self",
        Policies.EmployeesRead => "users:read",
        Policies.EmployeesCreate => "users:create",
        Policies.EmployeesUpdate => "users:update",
        Policies.EmployeesDelete => "users:delete",
        _ => null
    };

    private static bool HasAdminRole(ClaimsPrincipal user) =>
        HasRole(user, "Admin") ||
        HasRole(user, "SystemAdmin") ||
        HasRole(user, "TenantAdmin");

    private static bool HasRole(ClaimsPrincipal user, string role) =>
        ClaimsExtractor.GetRoles(user).Contains(role);

    private static bool HasPermission(ClaimsPrincipal user, string requiredPermission)
    {
        foreach (var permission in GetPermissionValues(user))
        {
            if (PermissionMatches(permission, requiredPermission))
                return true;
        }

        return false;
    }

    private static bool PermissionMatches(string permission, string requiredPermission)
    {
        if (string.Equals(permission, requiredPermission, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(permission, "admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(permission, "admin:*", StringComparison.OrdinalIgnoreCase))
            return true;

        var separatorIndex = permission.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex <= 0 || !permission.EndsWith(":*", StringComparison.Ordinal))
            return false;

        var permissionScope = permission[..separatorIndex];
        if (!requiredPermission.StartsWith(permissionScope + ":", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static bool HasAuthenticationMethod(ClaimsPrincipal user, string method)
    {
        var amr = ClaimsExtractor.GetAmr(user);
        if (string.IsNullOrWhiteSpace(amr))
            return false;

        return SplitClaimValues(amr).Contains(method, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> GetPermissionValues(ClaimsPrincipal user)
    {
        foreach (var permission in ClaimsExtractor.GetPermissions(user))
            yield return permission;

        foreach (var claim in user.Claims)
        {
            if (claim.Type is "scope" or "scp" or "http://schemas.gameguild.com/identity/claims/permission")
            {
                foreach (var permission in SplitClaimValues(claim.Value))
                    yield return permission;
            }
        }
    }

    private static IEnumerable<string> SplitClaimValues(string value) =>
        value.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
