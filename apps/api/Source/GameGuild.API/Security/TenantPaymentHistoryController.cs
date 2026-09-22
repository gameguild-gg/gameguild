using Asp.Versioning;
using GameGuild.Commerce.Payments;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Security;

/// <summary>
///     Composition-root controller bridging the tenants surface to the commerce payment
///     history query.
/// </summary>
/// <remarks>
///     This endpoint used to live in the Tenants module, which forced a platform module to
///     depend on the commerce stack. The dependency-direction rule now keeps platform
///     modules domain-free, so this product-specific bridge lives in the host while the
///     route stays exactly where tenants clients expect it.
/// </remarks>
[ApiVersion("1.0")]
[Microsoft.AspNetCore.Http.Tags("tenants")]
[Authorize]
public sealed class TenantPaymentHistoryController(ISender sender) : BaseApiController
{
    /// <summary>
    ///     Get payment history for tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="startDate">Optional start date filter for payment history</param>
    /// <param name="endDate">Optional end date filter for payment history</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Payment history</returns>
    [HttpGet("v{version:apiVersion}/tenants/{tenantId:guid}/payments")]
    [EndpointSummary("Get payment history for tenant")]
    [EndpointDescription("Retrieves payment history for a specific tenant with optional date filtering.")]
    [ProducesResponseType<IEnumerable<PaymentResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentHistory(Guid tenantId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetPaymentHistoryQuery(null, tenantId, startDate, endDate), ct));
    }
}
