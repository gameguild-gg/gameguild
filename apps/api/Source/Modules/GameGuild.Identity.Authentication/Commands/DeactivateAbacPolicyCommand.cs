using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record DeactivateAbacPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
