using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record ValidateConditionCommand : ICommand<ConditionValidationResult>
{
    public PolicyConditionType ConditionType { get; init; }

    public string ConditionJson { get; init; } = string.Empty;
}
