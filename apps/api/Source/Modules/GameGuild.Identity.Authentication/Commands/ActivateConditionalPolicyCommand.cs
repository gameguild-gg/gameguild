using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record ActivateConditionalPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
