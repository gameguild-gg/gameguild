using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record DeactivateConditionalPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
