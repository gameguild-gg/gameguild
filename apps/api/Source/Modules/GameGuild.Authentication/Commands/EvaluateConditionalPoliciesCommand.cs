using GameGuild.Authentication.DTOs;
using GameGuild.Authentication.Enums;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record EvaluateConditionalPoliciesCommand : ICommand<ConditionalPolicyResult>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public PermissionType? PermissionType { get; init; }

    public string? ResourceType { get; init; }

    public Dictionary<string, object> Context { get; init; } = new Dictionary<string, object>();
}
