using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record DeactivateConditionalPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
