using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetPermissionCacheStatsQuery : IQuery<PermissionCacheStatsDto> { }
