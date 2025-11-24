using GameGuild.Authentication.DTOs;
using GameGuild.Authentication.Models.Abac;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record BulkEvaluateAbacPoliciesCommand : ICommand<BulkAbacEvaluationResult>
{
    public List<AbacEvaluationContext> Contexts { get; init; } = new List<AbacEvaluationContext>();

    public List<Guid>? PolicyIds { get; init; }

    public Guid? TenantId { get; init; }
}
