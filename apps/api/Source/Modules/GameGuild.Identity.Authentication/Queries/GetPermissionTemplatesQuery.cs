using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetPermissionTemplatesQuery : IQuery<IEnumerable<PermissionTemplateDto>> { }
