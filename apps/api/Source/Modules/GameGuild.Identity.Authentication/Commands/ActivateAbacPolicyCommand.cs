using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record ActivateAbacPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
