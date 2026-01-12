using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetPolicyConditionTypesQuery : IQuery<IEnumerable<PolicyConditionTypeDto>> { }
