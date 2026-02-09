using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetPolicyConditionTypesQuery : IQuery<IEnumerable<PolicyConditionTypeDto>> { }
