using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record BulkEvaluateAbacPoliciesCommand : ICommand<BulkAbacEvaluationResult>
{
    public List<AbacEvaluationContext> Contexts { get; init; } = new List<AbacEvaluationContext>();

    public List<Guid>? PolicyIds { get; init; }

    public Guid? TenantId { get; init; }
}
