
using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Rule that requires ALL specified permissions.
///     Parameters:
///     - permissions: string[] - List of permission keys that are ALL required
/// </summary>
public sealed class RequireAllPermissionsRuleEvaluator : IRuleEvaluator
{
    private readonly IAuthorizationPermissionService _permissionService;
    private readonly IAuthorizationTenantContext _tenantContext;

    public RequireAllPermissionsRuleEvaluator(
        IAuthorizationPermissionService permissionService,
        IAuthorizationTenantContext tenantContext)
    {
        _permissionService = permissionService;
        _tenantContext = tenantContext;
    }

    public string RuleType => RuleTypes.RequireAllPermissions;

    public async Task<RuleEvaluationResult> EvaluateAsync(
        AuthorizationHandlerContext context,
        RuleParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var user = context.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return RuleEvaluationResult.Fail("User is not authenticated");
        }

        var requiredPermissions = parameters.GetStringArray("permissions");

        if (requiredPermissions.Count == 0)
        {
            return RuleEvaluationResult.Fail("At least one permission is required");
        }

        // Extract user ID from claims using centralized helper
        var userId = Utilities.ClaimsExtractor.GetUserIdAsGuid(user);
        if (!userId.HasValue)
        {
            return RuleEvaluationResult.Fail("Could not determine user ID from claims");
        }

        // System administrators have platform-wide permission authority. Keep the
        // remaining policy rules (for example MFA and tenant matching) intact,
        // while avoiding a tenant permission lookup that cannot represent this role.
        if (Utilities.ClaimsExtractor.GetRoles(user).Contains(Policies.SystemAdmin))
        {
            return RuleEvaluationResult.Success();
        }

        // Extract tenant ID from context (now Guid?) or claims
        Guid tenantId;
        if (_tenantContext.HasTenant && _tenantContext.TenantId.HasValue && _tenantContext.TenantId.Value != Guid.Empty)
        {
            tenantId = _tenantContext.TenantId.Value;
        }
        else
        {
            var tenantIdStr = Utilities.ClaimsExtractor.GetTenantId(user);
            if (!Guid.TryParse(tenantIdStr, out tenantId) || tenantId == Guid.Empty)
            {
                return RuleEvaluationResult.Fail("Could not determine tenant ID for permission check");
            }
        }

        // Use batch permission check (single DB call)
        var result = await _permissionService.HasAllPermissionsAsync(
            userId.Value, tenantId, requiredPermissions, cancellationToken).ConfigureAwait(false);

        if (!result.HasAllRequired)
        {
            return RuleEvaluationResult.Fail(
                $"Missing required permissions: {string.Join(", ", result.MissingPermissions)}");
        }

        return RuleEvaluationResult.Success();
    }
}

/// <summary>
///     Rule that requires ANY of the specified permissions (OR logic).
///     Parameters:
///     - permissions: string[] - List of permission keys where at least one is required
/// </summary>
public sealed class RequireAnyPermissionRuleEvaluator : IRuleEvaluator
{
    private readonly IAuthorizationPermissionService _permissionService;
    private readonly IAuthorizationTenantContext _tenantContext;

    public RequireAnyPermissionRuleEvaluator(
        IAuthorizationPermissionService permissionService,
        IAuthorizationTenantContext tenantContext)
    {
        _permissionService = permissionService;
        _tenantContext = tenantContext;
    }

    public string RuleType => RuleTypes.RequireAnyPermission;

    public async Task<RuleEvaluationResult> EvaluateAsync(
        AuthorizationHandlerContext context,
        RuleParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var user = context.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return RuleEvaluationResult.Fail("User is not authenticated");
        }

        var allowedPermissions = parameters.GetStringArray("permissions");

        if (allowedPermissions.Count == 0)
        {
            return RuleEvaluationResult.Fail("At least one permission is required");
        }

        // Extract user ID from claims using centralized helper
        var userId = Utilities.ClaimsExtractor.GetUserIdAsGuid(user);
        if (!userId.HasValue)
        {
            return RuleEvaluationResult.Fail("Could not determine user ID from claims");
        }

        // System administrators have platform-wide permission authority. Keep the
        // remaining policy rules (for example MFA and tenant matching) intact,
        // while avoiding a tenant permission lookup that cannot represent this role.
        if (Utilities.ClaimsExtractor.GetRoles(user).Contains(Policies.SystemAdmin))
        {
            return RuleEvaluationResult.Success();
        }

        // Extract tenant ID from context (now Guid?) or claims
        Guid tenantId;
        if (_tenantContext.HasTenant && _tenantContext.TenantId.HasValue && _tenantContext.TenantId.Value != Guid.Empty)
        {
            tenantId = _tenantContext.TenantId.Value;
        }
        else
        {
            var tenantIdStr = Utilities.ClaimsExtractor.GetTenantId(user);
            if (!Guid.TryParse(tenantIdStr, out tenantId) || tenantId == Guid.Empty)
            {
                return RuleEvaluationResult.Fail("Could not determine tenant ID for permission check");
            }
        }

        // Use batch permission check (single DB call)
        var result = await _permissionService.HasAnyPermissionAsync(
            userId.Value, tenantId, allowedPermissions, cancellationToken).ConfigureAwait(false);

        if (!result.HasAnyRequired)
        {
            return RuleEvaluationResult.Fail(
                $"None of the required permissions found: {string.Join(", ", allowedPermissions)}");
        }

        return RuleEvaluationResult.Success();
    }
}
