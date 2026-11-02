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
///     Cross-account device insight listing (issue #409: same device across multiple
///     accounts — signal plus listing surface). Authorization is enforced per request
///     through the <c>payments:admin</c> permission (<see cref="GetFraudDeviceInsightsQuery"/>)
///     in addition to the controller-level authentication requirement.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/fraud/devices")]
[Microsoft.AspNetCore.Http.Tags("fraud/devices")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
public sealed class FraudDevicesController(
    ISender sender,
    IActorContextAccessor actorContextAccessor) : BaseApiController
{
    /// <summary>
    ///     List device fingerprints shared across multiple customer accounts.
    /// </summary>
    /// <param name="tenantId">Optional tenant filter (system administrators only; other callers are scoped to their own tenant).</param>
    /// <param name="minAccounts">Minimum distinct accounts for a device to be listed; defaults to the assessment option (2).</param>
    /// <param name="lookbackDays">Look-back window in days; defaults to the assessment option (30).</param>
    /// <param name="page">Page number for pagination (1-based).</param>
    /// <param name="pageSize">Items per page (1-100).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [EndpointSummary("List cross-account devices")]
    [EndpointDescription("Lists device fingerprints presented on multiple customer accounts over a look-back window, with the per-account usage breakdown reviewers use to judge whether the sharing is a household or a fraud ring. Backs the Device.CrossAccountUsage fraud signal. Requires the payments:admin permission.")]
    [ProducesResponseType<IEnumerable<FraudDeviceInsight>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDevices(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] int? minAccounts = null,
        [FromQuery] int? lookbackDays = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var actorContext = actorContextAccessor.ActorContext;
        if (actorContext.IsAuthenticated && !actorContext.IsSystemAdmin)
        {
            tenantId = actorContext.TenantId;
        }

        return Ok(await sender.Send(
            new GetFraudDeviceInsightsQuery(tenantId, minAccounts, lookbackDays, page, pageSize),
            ct).ConfigureAwait(false));
    }
}
