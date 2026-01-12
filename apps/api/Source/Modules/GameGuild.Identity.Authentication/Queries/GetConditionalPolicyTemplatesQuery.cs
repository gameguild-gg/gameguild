using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetConditionalPolicyTemplatesQuery : IQuery<IEnumerable<ConditionalPolicyTemplateDto>> { }
