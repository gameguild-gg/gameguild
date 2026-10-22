using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Expanded per-gateway analytics (issue #413 analytics remainders): the rolling-window
///     aggregates plus the error-rate-by-type breakdown (failed payments grouped by gateway
///     error code), the per-gateway cost analysis (succeeded volume priced through the
///     configured fee schedule) and the geographic performance breakdown (attempts and
///     success rate per billing country). Requires the <c>payments:admin</c> permission; the
///     tenant scope is enforced at the API boundary from the actor context.
/// </summary>
/// <param name="TenantId">Optional tenant scope; enforced at the API boundary.</param>
[AuthorizeRequest(PaymentsPermission.Keys.Admin)]
public sealed record GetGatewayAnalyticsQuery(Guid? TenantId = null) : IQuery<GatewayAnalyticsReport>;

/// <summary>Handler for <see cref="GetGatewayAnalyticsQuery"/>.</summary>
public sealed class GetGatewayAnalyticsQueryHandler(
    IGatewayMetricsService gatewayMetrics) : IQueryHandler<GetGatewayAnalyticsQuery, GatewayAnalyticsReport>
{
    /// <inheritdoc />
    public async Task<GatewayAnalyticsReport> Handle(GetGatewayAnalyticsQuery request, CancellationToken cancellationToken)
    {
        return await gatewayMetrics
            .GetAnalyticsAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);
    }
}
