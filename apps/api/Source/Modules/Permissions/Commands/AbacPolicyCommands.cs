using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Commands;

/// <summary>
/// Command to evaluate ABAC policies
/// </summary>
public class EvaluateAbacPolicyCommand : IRequest<AbacEvaluationResult>
{
    public Guid UserId { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? ResourceId { get; init; }
    public string ResourceType { get; init; } = string.Empty;
    public PermissionType Permission { get; init; }
    public Dictionary<string, object> UserAttributes { get; init; } = new();
    public Dictionary<string, object> ResourceAttributes { get; init; } = new();
    public Dictionary<string, object> ContextAttributes { get; init; } = new();
}

/// <summary>
/// Command to create an ABAC policy
/// </summary>
public class CreateAbacPolicyCommand : IRequest<AbacPolicy>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? TenantId { get; init; }
    public string ResourceType { get; init; } = string.Empty;
    public PermissionType Permission { get; init; }
    public PolicyEffect Effect { get; init; } = PolicyEffect.Allow;
    public string AttributeExpression { get; init; } = "{}";
    public string? ConditionExpression { get; init; }
    public int Priority { get; init; } = 100;
    public bool IsActive { get; init; } = true;
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Command to update an ABAC policy
/// </summary>
public class UpdateAbacPolicyCommand : IRequest<AbacPolicy>
{
    public Guid PolicyId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ResourceType { get; init; }
    public PermissionType? Permission { get; init; }
    public PolicyEffect? Effect { get; init; }
    public string? AttributeExpression { get; init; }
    public string? ConditionExpression { get; init; }
    public int? Priority { get; init; }
    public bool? IsActive { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Command to delete an ABAC policy
/// </summary>
public class DeleteAbacPolicyCommand : IRequest<bool>
{
    public Guid PolicyId { get; init; }
}
