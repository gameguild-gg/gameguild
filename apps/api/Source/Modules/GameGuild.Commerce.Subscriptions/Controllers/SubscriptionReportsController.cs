using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameGuild.Commerce.Subscriptions;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[Microsoft.AspNetCore.Http.Tags("reports/subscriptions")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Api)]
public sealed class SubscriptionReportsController(ISender sender) : BaseApiController
{
    [HttpGet("churn")]
    [EndpointSummary("Get subscription churn and retention report")]
    [EndpointDescription("Calculates churn, retention, MRR, and subscription status breakdown for the selected period.")]
    [ProducesResponseType<SubscriptionChurnReportDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChurnReport(
        [FromQuery] Guid? tenantId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetSubscriptionChurnReportQuery(tenantId, startDate, endDate), ct).ConfigureAwait(false);
        return Ok(result);
    }
}
