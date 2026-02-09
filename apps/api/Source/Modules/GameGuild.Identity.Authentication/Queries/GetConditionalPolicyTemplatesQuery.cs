using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetConditionalPolicyTemplatesQuery : IQuery<IEnumerable<ConditionalPolicyTemplateDto>> { }
