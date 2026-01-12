using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record DeleteAbacPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
