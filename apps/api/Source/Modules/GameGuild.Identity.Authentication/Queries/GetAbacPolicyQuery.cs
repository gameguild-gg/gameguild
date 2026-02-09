using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetAbacPolicyQuery : IQuery<AbacPolicy>
{
    public Guid PolicyId { get; init; }
}
