using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record ActivateConditionalPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
