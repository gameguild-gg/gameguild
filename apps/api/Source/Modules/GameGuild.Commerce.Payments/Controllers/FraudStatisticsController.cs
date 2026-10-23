using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Fraud-program statistics (issue #409 reporting): assessed/flagged/blocked counts and
///     rates, the chargeback rate, the false-positive proxy, the manual-review backlog with
///     average time-to-review, and the top fraud signals by frequency. Authorization is
///     enforced per request through the <c>payments:admin</c> permission
///     (<see cref="GetFraudStatisticsQuery"/>) in addition to the controller-level
///     authentication requirement.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/fraud/statistics")]
[Microsoft.AspNetCore.Http.Tags("fraud/statistics")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
public sealed class FraudStatisticsController(
    ISender sender,
    IActorContextAccessor actorContextAccessor) : BaseApiController
{
    /// <summary>
    ///     Get the fraud statistics report for a date window (defaults to the last 30 days).
    /// </summary>
    /// <param name="tenantId">Optional tenant filter (system administrators only; other callers are scoped to their own tenant).</param>
    /// <param name="fromUtc">Optional inclusive window start (ISO-8601); defaults to 30 days ago.</param>
    /// <param name="toUtc">Optional inclusive window end (ISO-8601); defaults to now.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [EndpointSummary("Get fraud statistics")]
    [EndpointDescription("Reports fraud-program statistics over a date window: assessed, flagged and blocked payment counts with rates, the chargeback rate among succeeded payments, the false-positive proxy (held reviews later approved), the manual-review backlog with average time-to-review, and the top fraud signals by frequency. Requires the payments:admin permission.")]
    [ProducesResponseType<FraudStatisticsReport>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var actorContext = actorContextAccessor.ActorContext;
        if (actorContext.IsAuthenticated && !actorContext.IsSystemAdmin)
        {
            tenantId = actorContext.TenantId;
        }

        return Ok(await sender.Send(
            new GetFraudStatisticsQuery(tenantId, fromUtc, toUtc),
            ct).ConfigureAwait(false));
    }
}
