using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetPermissionTemplatesQuery : IQuery<IEnumerable<PermissionTemplateDto>> { }
