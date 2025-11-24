using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetAbacPolicyTemplatesQuery : IQuery<IEnumerable<AbacPolicyTemplateDto>> { }
