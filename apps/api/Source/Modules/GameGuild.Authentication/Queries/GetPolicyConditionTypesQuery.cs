using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetPolicyConditionTypesQuery : IQuery<IEnumerable<PolicyConditionTypeDto>> { }
