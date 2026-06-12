using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

// Legacy hidden shell; canonical access review analytics APIs are owned by GameGuild.Identity.Authorization.
/// <summary>
///     API controller for Access Review Analytics, Revocation, and Compliance.
///     Handles access revocation workflows, revocation history, analytics, and compliance reporting.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/access-reviews")]
[Microsoft.AspNetCore.Http.Tags("auth/access-reviews")]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
public class AccessReviewAnalyticsController(IMediator mediator, ILogger<AccessReviewAnalyticsController> logger)
    : BaseApiController
{
    private readonly ILogger<AccessReviewAnalyticsController> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    #region Access Revocation

    /// <summary>
    ///     Revoke access based on review decisions
    /// </summary>
    [HttpPost(":revoke-access")]
    public async Task<ActionResult<AccessRevocationResult>> RevokeAccess([FromBody] RevokeAccessCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Bulk revoke access for multiple users
    /// </summary>
    [HttpPost(":bulk-revoke-access")]
    public async Task<ActionResult<BulkAccessRevocationResult>> BulkRevokeAccess(
        [FromBody] BulkRevokeAccessCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Get access revocation history
    /// </summary>
    [HttpGet("revocation-history")]
    public async Task<ActionResult<PagedResult<AccessRevocationRecord>>> GetAccessRevocationHistory(
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? resourceId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50
    )
    {
        var query = new GetAccessRevocationHistoryQuery
        {
            UserId = userId,
            ResourceId = resourceId,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };
        var result = await _mediator.Send(query).ConfigureAwait(false);

        return Ok(result);
    }

    #endregion

    #region Access Review Analytics

    /// <summary>
    ///     Get access review analytics and compliance metrics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<ActionResult<AccessReviewAnalyticsDto>> GetAccessReviewAnalytics(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetAccessReviewAnalyticsQuery
        {
            TenantId = tenantId,
            FromDate = fromDate ?? SystemClock.UtcNow.AddMonths(-3),
            ToDate = toDate ?? SystemClock.UtcNow
        };
        var result = await _mediator.Send(query).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Get compliance status report
    /// </summary>
    [HttpGet("compliance-status")]
    public async Task<ActionResult<ComplianceStatusDto>> GetComplianceStatus([FromQuery] Guid tenantId)
    {
        var query = new GetComplianceStatusQuery { TenantId = tenantId };
        var result = await _mediator.Send(query).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Generate access review report
    /// </summary>
    [HttpPost(":generate-report")]
    public async Task<ActionResult<AccessReviewReportDto>> GenerateAccessReviewReport(
        [FromBody] GenerateAccessReviewReportCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return Ok(result);
    }

    #endregion
}
