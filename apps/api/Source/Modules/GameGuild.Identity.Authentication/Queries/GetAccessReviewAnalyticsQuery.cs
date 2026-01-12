using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetAccessReviewAnalyticsQuery : IQuery<AccessReviewAnalyticsDto>
{
    public Guid? TenantId { get; init; }

    public DateTime FromDate { get; init; }

    public DateTime ToDate { get; init; }
}
