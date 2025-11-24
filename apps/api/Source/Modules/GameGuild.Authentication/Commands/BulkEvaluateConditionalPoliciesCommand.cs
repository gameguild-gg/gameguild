using GameGuild.Authentication.DTOs;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record BulkEvaluateConditionalPoliciesCommand : ICommand<BulkConditionalPolicyResult>
{
    public List<ConditionalPolicyEvaluationRequest> Requests { get; init; } = new List<ConditionalPolicyEvaluationRequest>();
}
