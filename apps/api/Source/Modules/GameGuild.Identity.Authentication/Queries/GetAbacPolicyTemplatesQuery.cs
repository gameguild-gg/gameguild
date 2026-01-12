using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetAbacPolicyTemplatesQuery : IQuery<IEnumerable<AbacPolicyTemplateDto>> { }
