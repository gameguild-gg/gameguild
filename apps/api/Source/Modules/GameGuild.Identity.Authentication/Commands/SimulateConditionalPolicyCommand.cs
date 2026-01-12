using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record SimulateConditionalPolicyCommand : ICommand<ConditionalPolicySimulationResult>
{
    public Guid? PolicyId { get; init; }

    public PolicyConditionType? ConditionType { get; init; }

    public string? TimeConditions { get; init; }

    public string? EnvironmentConditions { get; init; }

    public string? LocationConditions { get; init; }

    public string? DeviceConditions { get; init; }

    public string? CustomConditions { get; init; }

    public List<Dictionary<string, object>> TestScenarios { get; init; } = new List<Dictionary<string, object>>();
}
