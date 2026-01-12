using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record UpdateAbacPolicyCommand : ICommand<AbacPolicy>
{
    public Guid PolicyId { get; set; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string JsonExpression { get; init; } = string.Empty;

    public AbacPolicyEffect Effect { get; init; }

    public int Priority { get; init; }

    public bool IsActive { get; init; }

    public string? Category { get; init; }

    public Dictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    public DateTime? EffectiveFrom { get; init; }

    public DateTime? EffectiveTo { get; init; }
}
