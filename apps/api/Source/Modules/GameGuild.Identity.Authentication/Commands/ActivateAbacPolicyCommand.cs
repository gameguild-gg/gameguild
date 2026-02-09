using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record ActivateAbacPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
