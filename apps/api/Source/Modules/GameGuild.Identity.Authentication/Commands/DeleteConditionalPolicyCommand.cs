using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record DeleteConditionalPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
