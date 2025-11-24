using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.Commands;

public abstract class ConditionalPolicyEvaluationRequest
{
    public Guid UserId { get; set; }

    public Guid TenantId { get; set; }

    public PermissionType? PermissionType { get; set; }

    public string? ResourceType { get; set; }

    public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
}
