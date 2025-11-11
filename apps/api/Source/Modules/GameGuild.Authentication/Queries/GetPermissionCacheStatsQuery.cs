using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetPermissionCacheStatsQuery : IQuery<PermissionCacheStatsDto> { }
