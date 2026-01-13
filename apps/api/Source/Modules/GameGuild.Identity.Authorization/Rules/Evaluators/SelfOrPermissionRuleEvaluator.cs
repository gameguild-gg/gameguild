
using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Rule that allows action if user is acting on themselves OR has a management permission.
///     Classic pattern: "edit self" OR "manage users"
///     Parameters:
///     - selfPermission: string - Permission required for self-action (e.g., "users:edit:self")
///     - anyPermission: string - Permission that allows action on anyone (e.g., "users:manage")
///     - resourceUserIdPath: string (optional) - Path to extract target user ID from resource (default: "UserId")
/// </summary>
public sealed class SelfOrPermissionRuleEvaluator : IRuleEvaluator
{
    private readonly IAuthorizationPermissionService _permissionService;
    private readonly IAuthorizationTenantContext _tenantContext;

    public SelfOrPermissionRuleEvaluator(
        IAuthorizationPermissionService permissionService,
        IAuthorizationTenantContext tenantContext)
    {
        _permissionService = permissionService;
        _tenantContext = tenantContext;
    }

    public string RuleType => RuleTypes.SelfOrPermission;

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

        // Extract user ID and tenant ID using centralized helpers
        var currentUserIdStr = Utilities.ClaimsExtractor.GetUserId(user);
        if (!Guid.TryParse(currentUserIdStr, out var currentUserId))
        {
            return RuleEvaluationResult.Fail("Could not determine current user ID");
        }

        var tenantIdStr = _tenantContext.TenantId ?? Utilities.ClaimsExtractor.GetTenantId(user);
        if (!Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return RuleEvaluationResult.Fail("Could not determine tenant ID for permission check");
        }

        var selfPermission = parameters.GetString("selfPermission");
        var anyPermission = parameters.GetString("anyPermission");

        // If user has the "any" permission, allow immediately
        if (!string.IsNullOrEmpty(anyPermission))
        {
            var hasAnyPermission = await _permissionService.HasPermissionAsync(
                currentUserId, tenantId, anyPermission, cancellationToken);
            if (hasAnyPermission)
            {
                return RuleEvaluationResult.Success();
            }
        }

        // Try to get target user ID from resource
        var targetUserId = GetTargetUserIdFromResource(context.Resource, parameters);

        if (string.IsNullOrEmpty(targetUserId))
        {
            // No resource - if selfPermission is set, check if user has it
            if (!string.IsNullOrEmpty(selfPermission))
            {
                var hasSelfPermission = await _permissionService.HasPermissionAsync(
                    currentUserId, tenantId, selfPermission, cancellationToken);
                if (hasSelfPermission)
                {
                    return RuleEvaluationResult.Success();
                }
            }

            return RuleEvaluationResult.Fail(
                "Cannot determine target user and user lacks required permissions");
        }

        // Check if acting on self
        var isSelf = string.Equals(currentUserIdStr, targetUserId, StringComparison.OrdinalIgnoreCase);

        if (isSelf)
        {
            // For self-action, check selfPermission if specified
            if (string.IsNullOrEmpty(selfPermission))
            {
                // No self permission required - self-action is allowed
                return RuleEvaluationResult.Success();
            }

            var hasSelfPermission = await _permissionService.HasPermissionAsync(
                currentUserId, tenantId, selfPermission, cancellationToken);
            if (hasSelfPermission)
            {
                return RuleEvaluationResult.Success();
            }

            return RuleEvaluationResult.Fail(
                $"Self-action requires permission '{selfPermission}'");
        }

        // Not self, and already checked anyPermission above
        return RuleEvaluationResult.Fail(
            $"Action on other users requires permission '{anyPermission}'");
    }

    private static string? GetTargetUserIdFromResource(object? resource, RuleParameters parameters)
    {
        if (resource is null)
            return null;

        var userIdPath = parameters.GetString("resourceUserIdPath") ?? "UserId";

        // Try to get from IUserIdResource interface
        if (resource is IUserIdResource userIdResource)
        {
            return userIdResource.UserId?.ToString();
        }

        // Try reflection
        var property = resource.GetType().GetProperty(userIdPath);
        if (property is not null)
        {
            var value = property.GetValue(resource);
            return value?.ToString();
        }

        // Try dictionary
        if (resource is IDictionary<string, object> dict && dict.TryGetValue(userIdPath, out var val))
        {
            return val.ToString();
        }

        return null;
    }
}

/// <summary>
///     Interface for resources that have a user ID.
/// </summary>
public interface IUserIdResource
{
    /// <summary>
    ///     The user ID associated with this resource.
    /// </summary>
    Guid? UserId { get; }
}
