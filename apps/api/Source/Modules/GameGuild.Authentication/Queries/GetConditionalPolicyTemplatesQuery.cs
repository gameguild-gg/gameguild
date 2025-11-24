using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetConditionalPolicyTemplatesQuery : IQuery<IEnumerable<ConditionalPolicyTemplateDto>> { }
