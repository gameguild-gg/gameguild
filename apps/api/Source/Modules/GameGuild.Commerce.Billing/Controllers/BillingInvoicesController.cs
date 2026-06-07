using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameGuild.Commerce.Billing;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/billing/invoices")]
[Microsoft.AspNetCore.Http.Tags("billing/invoices")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Api)]
public sealed class BillingInvoicesController(ISender sender) : BaseApiController
{
    [HttpPost("{invoiceId:guid}/retry")]
    [EndpointSummary("Retry invoice payment")]
    [EndpointDescription("Accepts a local retry scheduling request for open or past-due invoices. External gateway capture requires configured payment-provider credentials.")]
    [ProducesResponseType<InvoicePaymentRetryResult>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<InvoicePaymentRetryResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<InvoicePaymentRetryResult>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<InvoicePaymentRetryResult>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetryPayment(Guid invoiceId, CancellationToken ct)
    {
        var result = await sender.Send(new RetryInvoicePaymentCommand(invoiceId), ct).ConfigureAwait(false);

        return result.Code switch
        {
            "NotFound" => NotFound(result),
            "RetryAccepted" => Accepted(result),
            "RetryLimitExceeded" => Conflict(result),
            _ => BadRequest(result)
        };
    }
}
