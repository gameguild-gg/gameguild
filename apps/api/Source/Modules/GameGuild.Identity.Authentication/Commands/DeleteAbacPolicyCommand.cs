using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record DeleteAbacPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
