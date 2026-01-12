using GameGuild.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Authentication;

// ABAC Policy Commands
public record CreateAbacPolicyCommand : ICommand<AbacPolicy>
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public Guid TenantId { get; init; }

    public string JsonExpression { get; init; } = string.Empty;

    public AbacPolicyEffect Effect { get; init; }

    public int Priority { get; init; }

    public bool IsActive { get; init; } = true;

    public string? Category { get; init; }

    public Dictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    public DateTime? EffectiveFrom { get; init; }

    public DateTime? EffectiveTo { get; init; }
}

// ABAC Policy Evaluation Commands

// ABAC Policy Validation Commands

// ABAC Policy Template Commands
