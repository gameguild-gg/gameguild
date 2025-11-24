using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record DeactivateAbacPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
