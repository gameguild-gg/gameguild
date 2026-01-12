
using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Rule that checks resource ownership OR ACL access.
///     Parameters:
///     - permission: string (optional) - Permission to check in ACL (e.g., "doc:edit")
///     - minimumAccessLevel: string (optional) - Minimum ACL access level ("Read", "Write", "Admin")
///     - allowOwner: bool (optional, default: true) - Whether owner bypasses ACL check
/// </summary>
public sealed class OwnerOrAclRuleEvaluator(IAccessControlListService aclService) : IRuleEvaluator
{
    public string RuleType => RuleTypes.OwnerOrAcl;

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

        var resource = context.Resource;
        if (resource is null)
        {
            // No resource - skip this rule (can't check ownership/ACL)
            return RuleEvaluationResult.Skip("No resource provided for ownership/ACL check");
        }

        var allowOwner = parameters.GetBool("allowOwner", true);

        // Check ownership first (if enabled)
        if (allowOwner && resource is IOwnedResource ownedResource)
        {
            if (ClaimNames.TryGetUserId(user, out var userGuid) &&
                ownedResource.OwnerId == userGuid)
            {
                return RuleEvaluationResult.Success();
            }
        }

        // Check ACL
        if (resource is IAccessControlListResource aclResource)
        {
            var minimumAccessLevelStr = parameters.GetString("minimumAccessLevel") ?? "Read";
            var minimumAccessLevel = Enum.TryParse<AccessLevel>(minimumAccessLevelStr, true, out var level)
                ? level
                : AccessLevel.Read;

            // Build ACL subject from user claims
            var subject = BuildAclSubject(user);

            // Get tenant ID from claims
            if (!ClaimNames.TryGetTenantId(user, out var tenantId))
            {
                return RuleEvaluationResult.Fail("Could not determine tenant for ACL check");
            }

            var hasAccess = await aclService.HasAccessAsync(
                subject,
                tenantId,
                aclResource.ResourceType,
                aclResource.ResourceId,
                minimumAccessLevel,
                cancellationToken);

            if (hasAccess)
            {
                return RuleEvaluationResult.Success();
            }

            return RuleEvaluationResult.Fail(
                $"User does not have {minimumAccessLevel} access to resource '{aclResource.ResourceId}'");
        }

        return RuleEvaluationResult.Fail("Resource does not support ownership or ACL checks");
    }

    private static AclSubject BuildAclSubject(System.Security.Claims.ClaimsPrincipal user)
    {
        ClaimNames.TryGetUserId(user, out var userGuid);

        var roleIds = user.FindAll(ClaimNames.Role)
            .Select(c => Guid.TryParse(c.Value, out var rid) ? rid : (Guid?)null)
            .Where(r => r.HasValue)
            .Select(r => r!.Value)
            .ToList();

        var groupIds = user.FindAll(ClaimNames.Group)
            .Select(c => Guid.TryParse(c.Value, out var gid) ? gid : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        return new AclSubject
        {
            UserId = userGuid == Guid.Empty ? null : userGuid,
            RoleIds = roleIds,
            GroupIds = groupIds,
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false
        };
    }
}
