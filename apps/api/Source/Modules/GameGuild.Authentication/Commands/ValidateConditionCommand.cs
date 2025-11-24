using GameGuild.Authentication.DTOs;
using GameGuild.Authentication.Entities;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record ValidateConditionCommand : ICommand<ConditionValidationResult>
{
    public PolicyConditionType ConditionType { get; init; }

    public string ConditionJson { get; init; } = string.Empty;
}
