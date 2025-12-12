using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetPermissionTemplatesQuery : IQuery<IEnumerable<PermissionTemplateDto>> { }
