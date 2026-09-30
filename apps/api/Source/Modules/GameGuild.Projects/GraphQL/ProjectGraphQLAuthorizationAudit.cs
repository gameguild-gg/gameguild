namespace GameGuild.Projects;

/// <summary>Records authorization denials produced by the Projects GraphQL schema.</summary>
public interface IProjectGraphQLAuthorizationAuditSink
{
    Task RecordDeniedAsync(ProjectGraphQLAuthorizationDenial denial, CancellationToken cancellationToken);
}

/// <summary>Safe, structured details for a denied Projects GraphQL field.</summary>
public sealed record ProjectGraphQLAuthorizationDenial(
    Guid? ActorId,
    Guid? TenantId,
    Guid? ProjectId,
    string FieldName,
    IReadOnlyCollection<string> RequiredPermissions,
    string Reason);
