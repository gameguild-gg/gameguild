using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record BulkEvaluateConditionalPoliciesCommand : ICommand<BulkConditionalPolicyResult>
{
    public List<ConditionalPolicyEvaluationRequest> Requests { get; init; } = new List<ConditionalPolicyEvaluationRequest>();
}
