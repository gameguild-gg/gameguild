using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetAbacPolicyTemplatesQuery : IQuery<IEnumerable<AbacPolicyTemplateDto>> { }
