using GameGuild.Identity.Authentication;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.Audit;

/// <summary>
///     Bridges role-assignment lifecycle events (assign / revoke, issue #1204) to the
///     central audit log under the <see cref="AuditActionTypes.RoleAssigned"/> /
/// <see cref="AuditActionTypes.RoleRevoked"/> taxonomy keys — the action types the
/// compliance event-coverage checks expect but that previously had no producer. Role
/// mutations are required audit events: they are recorded in the owning command's
/// transaction and fail closed, exactly like the other module sinks.
/// </summary>
public sealed class CentralRoleAssignmentAuditEventSink(
    IApplicationDbContext context,
    IActorContextAccessor actors,
    IHttpContextAccessor httpContextAccessor) : IRoleAssignmentAuditEventSink
{
    public Task RecordAsync(RoleAssignmentAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        var actor = actors.ActorContext;
        var isAssignment = auditEvent.ActionType == RoleAssignmentAuditActionTypes.RoleAssigned;

        return RequiredModuleAuditRecorder.RecordAsync(
            context,
            actors,
            httpContextAccessor.HttpContext,
            new CreateAuditLogRequest
            {
                ActionType = auditEvent.ActionType,
                ResourceType = "Role",
                ResourceId = auditEvent.RoleId.ToString(),
                UserId = auditEvent.SubjectUserId,
                TenantId = auditEvent.TenantId,
                Description = isAssignment
                    ? $"Role '{auditEvent.RoleName}' assigned to user {auditEvent.SubjectUserId}"
                    : $"Role '{auditEvent.RoleName}' revoked from user {auditEvent.SubjectUserId}",
                Metadata = new
                {
                    RoleName = auditEvent.RoleName,
                    auditEvent.PerformedBy,
                    auditEvent.ExpiresAt,
                    ActingUserId = actor.SubjectIdAsGuid,
                    Details = auditEvent.Metadata
                },
                Success = auditEvent.Success,
                ErrorMessage = auditEvent.ErrorMessage,
                RiskLevel = AuditRiskLevel.Medium,
                Category = AuditCategory.Authorization
            },
            "identity.roles",
            cancellationToken);
    }
}
