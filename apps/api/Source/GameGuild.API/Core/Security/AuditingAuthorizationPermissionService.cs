using System.Security.Claims;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace GameGuild.API.Core.Security;

/// <summary>
/// Records permission-service decisions in the shared audit trail without changing their outcomes.
/// </summary>
internal sealed class AuditingAuthorizationPermissionService(
    AuthorizationPermissionServiceAdapter inner,
    IAuditService auditService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditingAuthorizationPermissionService> logger)
    : IAuthorizationPermissionService
{
    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid tenantId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var granted = await inner.HasPermissionAsync(userId, tenantId, permission, cancellationToken)
            .ConfigureAwait(false);
        await LogDecisionSafelyAsync(userId, tenantId, permission, granted, "HasPermission")
            .ConfigureAwait(false);

        return granted;
    }

    public async Task<PermissionCheckResult> HasAllPermissionsAsync(
        Guid userId,
        Guid tenantId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var permissionList = permissions.ToArray();
        var result = await inner.HasAllPermissionsAsync(userId, tenantId, permissionList, cancellationToken)
            .ConfigureAwait(false);
        await LogBatchDecisionsSafelyAsync(
                userId,
                tenantId,
                permissionList,
                result.PresentPermissions,
                "HasAllPermissions")
            .ConfigureAwait(false);

        return result;
    }

    public async Task<PermissionCheckResult> HasAnyPermissionAsync(
        Guid userId,
        Guid tenantId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var permissionList = permissions.ToArray();
        var result = await inner.HasAnyPermissionAsync(userId, tenantId, permissionList, cancellationToken)
            .ConfigureAwait(false);
        await LogBatchDecisionsSafelyAsync(
                userId,
                tenantId,
                permissionList,
                result.PresentPermissions,
                "HasAnyPermission")
            .ConfigureAwait(false);

        return result;
    }

    public Task<IReadOnlyList<string>> GetPermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
        => inner.GetPermissionsAsync(userId, tenantId, cancellationToken);

    private async Task LogBatchDecisionsSafelyAsync(
        Guid userId,
        Guid tenantId,
        IReadOnlyCollection<string> requestedPermissions,
        IReadOnlyCollection<string> grantedPermissions,
        string evaluationType)
    {
        var granted = new HashSet<string>(grantedPermissions, StringComparer.OrdinalIgnoreCase);

        foreach (var permission in requestedPermissions)
        {
            await LogDecisionSafelyAsync(
                    userId,
                    tenantId,
                    permission,
                    granted.Contains(permission),
                    evaluationType)
                .ConfigureAwait(false);
        }
    }

    private async Task LogDecisionSafelyAsync(
        Guid subjectUserId,
        Guid tenantId,
        string permission,
        bool granted,
        string evaluationType)
    {
        var context = httpContextAccessor.HttpContext;

        try
        {
            await auditService.LogAsync(new CreateAuditLogRequest
            {
                ActionType = granted ? AuditActionTypes.PermissionGranted : AuditActionTypes.PermissionDenied,
                ResourceType = "Permission",
                ResourceId = permission,
                UserId = GetActorUserId(context?.User) ?? subjectUserId,
                TenantId = tenantId,
                SessionId = GetGuidClaim(context?.User, JwtClaimTypes.SessionId, "session_id"),
                IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context?.Request.Headers.UserAgent.ToString(),
                Description = granted
                    ? $"Permission '{permission}' was granted in tenant scope."
                    : $"Permission '{permission}' was denied in tenant scope.",
                Metadata = new
                {
                    Permission = permission,
                    SubjectUserId = subjectUserId,
                    EvaluationType = evaluationType,
                    HttpMethod = context?.Request.Method,
                    RouteTemplate = (context?.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText
                },
                Success = granted,
                ErrorMessage = granted ? null : "The requested permission was not present in the tenant scope.",
                RiskLevel = granted ? AuditRiskLevel.Medium : AuditRiskLevel.High,
                Category = AuditCategory.Permission,
                CorrelationId = context?.TraceIdentifier
            }).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Audit persistence is best-effort and must not alter the permission decision.
            logger.LogError(
                exception,
                "Could not record authorization permission decision for {Permission}",
                permission);
        }
    }

    private static Guid? GetActorUserId(ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal?.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static Guid? GetGuidClaim(ClaimsPrincipal? principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal?.FindFirst(claimType)?.Value;
            if (Guid.TryParse(value, out var id))
            {
                return id;
            }
        }

        return null;
    }
}
