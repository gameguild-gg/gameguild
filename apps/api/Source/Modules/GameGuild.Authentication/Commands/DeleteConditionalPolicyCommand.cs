using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record DeleteConditionalPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
