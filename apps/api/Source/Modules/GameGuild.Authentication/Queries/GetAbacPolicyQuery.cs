using GameGuild.Authentication.Entities;
using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetAbacPolicyQuery : IQuery<AbacPolicy>
{
    public Guid PolicyId { get; init; }
}
