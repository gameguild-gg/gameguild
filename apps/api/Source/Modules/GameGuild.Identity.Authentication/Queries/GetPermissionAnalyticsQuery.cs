using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetPermissionAnalyticsQuery : IQuery<PermissionAnalyticsDto>
{
    public Guid TenantId { get; init; }

    public DateTime FromDate { get; init; }

    public DateTime ToDate { get; init; }
}
