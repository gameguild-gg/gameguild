using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record DeleteAbacPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
