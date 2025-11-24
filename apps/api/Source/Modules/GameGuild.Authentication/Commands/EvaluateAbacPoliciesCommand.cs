using GameGuild.Authentication.Models.Abac;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record EvaluateAbacPoliciesCommand : ICommand<AbacEvaluationResult>
{
    public AbacEvaluationContext Context { get; init; } = new AbacEvaluationContext();

    public List<Guid>? PolicyIds { get; init; }

    public Guid? TenantId { get; init; }
}
