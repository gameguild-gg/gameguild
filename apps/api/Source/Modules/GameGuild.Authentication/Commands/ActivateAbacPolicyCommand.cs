using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record ActivateAbacPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
