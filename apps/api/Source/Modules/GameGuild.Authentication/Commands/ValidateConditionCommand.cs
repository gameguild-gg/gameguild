using GameGuild.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Authentication;

public record ValidateConditionCommand : ICommand<ConditionValidationResult>
{
    public PolicyConditionType ConditionType { get; init; }

    public string ConditionJson { get; init; } = string.Empty;
}
