using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetAbacPolicyQuery : IQuery<AbacPolicy>
{
    public Guid PolicyId { get; init; }
}
