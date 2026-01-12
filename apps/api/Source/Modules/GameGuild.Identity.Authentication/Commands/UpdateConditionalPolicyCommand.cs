using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record UpdateConditionalPolicyCommand : ICommand<ConditionalPolicy>
{
    public Guid PolicyId { get; set; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public PolicyConditionType ConditionType { get; init; }

    public PermissionType? PermissionType { get; init; }

    public string? ResourceType { get; init; }

    public PolicyAction Action { get; init; }

    public int Priority { get; init; }

    public bool IsEnabled { get; init; }

    public string? TimeConditions { get; init; }

    public string? EnvironmentConditions { get; init; }

    public string? LocationConditions { get; init; }

    public string? DeviceConditions { get; init; }

    public string? CustomConditions { get; init; }
}
