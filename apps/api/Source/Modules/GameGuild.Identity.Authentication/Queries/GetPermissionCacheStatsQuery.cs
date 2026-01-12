using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetPermissionCacheStatsQuery : IQuery<PermissionCacheStatsDto> { }
