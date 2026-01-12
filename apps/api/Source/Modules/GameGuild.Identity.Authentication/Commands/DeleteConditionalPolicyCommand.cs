using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record DeleteConditionalPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
