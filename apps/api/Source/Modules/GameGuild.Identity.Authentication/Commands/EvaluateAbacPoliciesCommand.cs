using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record EvaluateAbacPoliciesCommand : ICommand<AbacEvaluationResult>
{
    public AbacEvaluationContext Context { get; init; } = new AbacEvaluationContext();

    public List<Guid>? PolicyIds { get; init; }

    public Guid? TenantId { get; init; }
}
