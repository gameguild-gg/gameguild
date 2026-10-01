using GameGuild.Compliance.Audit;
using GameGuild.Projects;

namespace GameGuild.API.Projects;

internal sealed class ProjectGraphQLAuthorizationAuditSink(IAuditService auditService)
    : IProjectGraphQLAuthorizationAuditSink
{
    public Task RecordDeniedAsync(
        ProjectGraphQLAuthorizationDenial denial,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return auditService.LogAsync(new CreateAuditLogRequest
        {
            ActionType = AuditActionTypes.PermissionDenied,
            ResourceType = "Project",
            ResourceId = denial.ProjectId?.ToString("D"),
            UserId = denial.ActorId,
            TenantId = denial.TenantId,
            Description = $"GraphQL access denied for field '{denial.FieldName}'.",
            Metadata = new
            {
                denial.FieldName,
                denial.RequiredPermissions,
                denial.Reason,
            },
            Success = false,
            ErrorMessage = denial.Reason,
            RiskLevel = AuditRiskLevel.High,
            Category = AuditCategory.Permission,
        });
    }
}
