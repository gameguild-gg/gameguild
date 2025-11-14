using Asp.Versioning;
using GameGuild.Billing.Commands;
using GameGuild.Billing.DTOs;
using GameGuild.Billing.Exceptions;
using GameGuild.Billing.Models;
using GameGuild.Billing.Queries;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Billing.Controllers;

/// <summary>
///     Controller for handling billing webhooks from external payment providers
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/billing/webhooks")]
[AllowAnonymous]
public sealed class BillingWebhooksController(ISender sender, ILogger<BillingWebhooksController> logger) : ControllerBase
{
    /// <summary>
    ///     Handle Google Pay webhook events for transaction notifications
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Webhook processing confirmation</returns>
    /// <remarks>
    ///     Processes Google Pay webhook notifications for payment processing, subscription billing,
    ///     and transaction status updates. Google Pay webhooks provide real-time notifications for
    ///     payment completions, failures, refunds, and subscription lifecycle events.
    ///     Google Pay webhook events include:
    ///     - Payment authorization and capture events
    ///     - Subscription creation and renewal notifications
    ///     - Refund and chargeback notifications
    ///     - Payment method updates and changes
    ///     - Account and billing profile modifications
    ///     Authentication and Security:
    ///     - Google Pay webhooks use JWT-based authentication
    ///     - Webhook signatures should be verified using Google's public keys
    ///     - Payload verification ensures event authenticity and prevents replay attacks
    ///     Required Headers:
    ///     - Authorization: Bearer token for webhook authentication
    ///     - Google-Cloud-Project-Id: Project identifier for multi-tenant validation
    /// </remarks>
    [HttpPost("google-pay")]
    [EndpointSummary("Handle Google Pay webhook events for transaction notifications")]
    [EndpointDescription(
        "Processes Google Pay webhook notifications for payment processing, subscription billing, and transaction status updates. Google Pay webhooks provide real-time notifications for payment completions, failures, refunds, and subscription lifecycle events."
    )]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HandleGooglePayWebhook(CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync(ct);

            // Get authorization header for JWT verification
            var authHeader = Request.Headers["Authorization"].ToString();
            var projectId = Request.Headers["Google-Cloud-Project-Id"].ToString();

            if (string.IsNullOrEmpty(authHeader))
            {
                logger.LogWarning("Missing Google Pay authorization header");

                return BadRequest(new { error = "Missing authorization header" });
            }

            if (string.IsNullOrEmpty(projectId))
            {
                logger.LogWarning("Missing Google Cloud Project ID header");

                return BadRequest(new { error = "Missing project ID header" });
            }

            logger.LogInformation("Processing Google Pay webhook for project: {ProjectId}", projectId);

            var result = await sender.Send(new ProcessGooglePayWebhookCommand(payload, authHeader, projectId), ct).ConfigureAwait(false);

            return Ok(new { received = true, processed = result.Processed });
        }
        catch (InvalidWebhookSignatureException ex)
        {
            logger.LogWarning("Invalid Google Pay webhook signature: {Message}", ex.Message);

            return Unauthorized(new { error = "Invalid signature" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Google Pay webhook");

            return StatusCode(500, new { error = "Webhook processing failed" });
        }
    }

    /// <summary>
    ///     Handle Stripe webhook events with signature verification
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Webhook processing confirmation</returns>
    /// <remarks>
    ///     Processes Stripe webhook notifications with enhanced security through signature verification.
    ///     Handles subscription lifecycle events, payment confirmations, invoice updates, and customer changes.
    ///     Stripe signatures are verified using the webhook signing secret to ensure event authenticity.
    ///     Required Headers:
    ///     - Stripe-Signature: The signature provided by Stripe for webhook verification
    /// </remarks>
    [HttpPost("stripe")]
    [EndpointSummary("Handle Stripe webhook events with signature verification")]
    [EndpointDescription(
        "Processes Stripe webhook notifications with enhanced security through signature verification. Handles subscription lifecycle events, payment confirmations, invoice updates, and customer changes. Stripe signatures are verified using the webhook signing secret to ensure event authenticity."
    )]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleStripeWebhook(CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync(ct);

            var signature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrEmpty(signature))
            {
                logger.LogWarning("Missing Stripe signature header");

                return BadRequest(new { error = "Missing signature" });
            }

            var headers = new Dictionary<string, string> { ["Stripe-Signature"] = signature };

            var result = await sender.Send(new ProcessStripeWebhookCommand(payload, signature), ct).ConfigureAwait(false);

            return Ok(new { received = true });
        }
        catch (InvalidWebhookSignatureException)
        {
            logger.LogWarning("Invalid Stripe webhook signature");

            return BadRequest(new { error = "Invalid signature" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Stripe webhook");

            return StatusCode(500);
        }
    }

    /// <summary>
    ///     Handle PayPal IPN (Instant Payment Notification) webhook events
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Webhook processing confirmation</returns>
    /// <remarks>
    ///     Processes PayPal Instant Payment Notification (IPN) webhook events for subscription billing,
    ///     payment confirmations, and account updates. PayPal IPN provides real-time transaction status
    ///     updates and subscription lifecycle management for PayPal-based billing integrations.
    ///     Note: PayPal IPN requires additional verification by sending the payload back to PayPal for validation.
    /// </remarks>
    [HttpPost("paypal")]
    [EndpointSummary("Handle PayPal IPN (Instant Payment Notification) webhook events")]
    [EndpointDescription(
        "Processes PayPal Instant Payment Notification (IPN) webhook events for subscription billing, payment confirmations, and account updates. PayPal IPN provides real-time transaction status updates and subscription lifecycle management for PayPal-based billing integrations."
    )]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HandlePayPalWebhook(CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync(ct);

            var result = await sender.Send(new ProcessPayPalWebhookCommand(payload), ct).ConfigureAwait(false);

            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing PayPal webhook");

            return StatusCode(500);
        }
    }

    /// <summary>
    ///     Retrieve webhook event details by event ID
    /// </summary>
    /// <param name="eventId">The unique identifier of the webhook event</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Webhook event details including payload, status, and processing information</returns>
    /// <remarks>
    ///     Retrieves detailed information about a specific webhook event for debugging and monitoring purposes.
    ///     Shows event payload, processing status, timestamps, and any error messages.
    ///     Useful for troubleshooting webhook processing issues and verifying event delivery.
    ///     Response includes:
    ///     - Event ID and timestamp
    ///     - Original webhook payload
    ///     - Processing status and results
    ///     - Error messages (if any)
    ///     - Provider information
    /// </remarks>
    [HttpGet("events/{eventId}")]
    [EndpointSummary("Retrieve webhook event details by event ID")]
    [EndpointDescription(
        "Retrieves detailed information about a specific webhook event for debugging and monitoring purposes. Shows event payload, processing status, timestamps, and any error messages. Useful for troubleshooting webhook processing issues and verifying event delivery."
    )]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWebhookEvent(string eventId, CancellationToken ct)
    {
        var webhookEvent = await sender.Send(new GetWebhookEventQuery(eventId), ct).ConfigureAwait(false);

        return webhookEvent is null ? NotFound() : Ok(webhookEvent);
    }

    /// <summary>
    ///     Retry failed webhook event processing
    /// </summary>
    /// <param name="eventId">The unique identifier of the failed webhook event to retry</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Retry operation result with success status and updated processing information</returns>
    /// <remarks>
    ///     Manually retries processing of a previously failed webhook event. Useful for handling temporary failures
    ///     such as downstream service unavailability, network timeouts, or transient processing errors.
    ///     The retry operation uses the original event payload and applies current business logic.
    ///     Common retry scenarios:
    ///     - Temporary network connectivity issues
    ///     - Downstream service unavailability
    ///     - Database connection timeouts
    ///     - Rate limiting from external services
    ///     Note: Only failed events can be retried. Successfully processed events will return an error.
    /// </remarks>
    [HttpPatch("events/{eventId}/retry")]
    [EndpointSummary("Retry failed webhook event processing")]
    [EndpointDescription(
        "Manually retries processing of a previously failed webhook event. Useful for handling temporary failures such as downstream service unavailability, network timeouts, or transient processing errors. The retry operation uses the original event payload and applies current business logic."
    )]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryWebhookEvent(string eventId, CancellationToken ct)
    {
        var result = await sender.Send(new RetryWebhookEventCommand(eventId), ct).ConfigureAwait(false);

        return result.Success ? Ok(result) : NotFound();
    }
}
