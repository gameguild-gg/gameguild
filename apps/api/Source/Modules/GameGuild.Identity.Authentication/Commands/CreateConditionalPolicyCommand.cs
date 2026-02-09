using GameGuild.Identity.Authorization;
using GameGuild.CQRS;
using GameGuild.Resources;

namespace GameGuild.Identity.Authentication;

// Conditional Policy CRUD Commands
[RequiresQuota(ResourceUsageType.ConditionalPolicies, 1, Source = "CreateConditionalPolicy")]
public sealed record CreateConditionalPolicyCommand : ICommand<ConditionalPolicy>
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid TenantId { get; init; }

    public PolicyConditionType ConditionType { get; init; }

    public PermissionType? PermissionType { get; init; }

    public string? ResourceType { get; init; }

    public PolicyAction Action { get; init; }

    public int Priority { get; init; } = 0;

    public bool IsEnabled { get; init; } = true;

    public string? TimeConditions { get; init; }

    public string? EnvironmentConditions { get; init; }

    public string? LocationConditions { get; init; }

    public string? DeviceConditions { get; init; }

    public string? CustomConditions { get; init; }
}

// Conditional Policy Evaluation Commands

// Conditional Policy Validation Commands

// Conditional Policy Simulation Commands

// Conditional Policy Template Commands

// Supporting DTOs for evaluation requests (non-response classes only)
