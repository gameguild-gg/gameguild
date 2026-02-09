using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetConditionalPolicyQuery : IQuery<ConditionalPolicy>
{
    public Guid PolicyId { get; init; }
}
