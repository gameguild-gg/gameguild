using GameGuild.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Authentication;

public record ValidateConditionalPolicyCommand : ICommand<ConditionalPolicyValidationResult>
{
    public PolicyConditionType ConditionType { get; init; }

    public string? TimeConditions { get; init; }

    public string? EnvironmentConditions { get; init; }

    public string? LocationConditions { get; init; }

    public string? DeviceConditions { get; init; }

    public string? CustomConditions { get; init; }
}
