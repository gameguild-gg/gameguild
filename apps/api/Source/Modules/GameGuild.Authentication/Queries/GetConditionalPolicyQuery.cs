using GameGuild.Authentication.Entities;
using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetConditionalPolicyQuery : IQuery<ConditionalPolicy>
{
    public Guid PolicyId { get; init; }
}
