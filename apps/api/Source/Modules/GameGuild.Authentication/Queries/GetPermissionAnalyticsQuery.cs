using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetPermissionAnalyticsQuery : IQuery<PermissionAnalyticsDto>
{
    public Guid TenantId { get; init; }

    public DateTime FromDate { get; init; }

    public DateTime ToDate { get; init; }
}
