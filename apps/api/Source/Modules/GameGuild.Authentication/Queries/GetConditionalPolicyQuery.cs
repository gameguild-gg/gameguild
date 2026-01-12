using GameGuild.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Authentication;

public record GetConditionalPolicyQuery : IQuery<ConditionalPolicy>
{
    public Guid PolicyId { get; init; }
}
