using GameGuild.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Authentication;

public record GetAbacPolicyQuery : IQuery<AbacPolicy>
{
    public Guid PolicyId { get; init; }
}
