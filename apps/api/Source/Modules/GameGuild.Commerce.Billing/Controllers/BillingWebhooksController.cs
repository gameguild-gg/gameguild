using Asp.Versioning;


using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Controller for handling billing webhooks from external payment providers.
///     Only the provider callback actions are anonymous (each is registered in the
///     anonymous endpoint registry and authenticated by provider signatures/secrets
///     plus the webhook source security filter); event inspection, manual retry, and
///     the security monitoring surface require the SystemAdmin policy.
///     Every provider callback enforces the explicit sub-5-second response bound
///     (issue #408): processing that exceeds <see cref="WebhookSettings.ResponseBoundSeconds"/>
///     is acknowledged as received-and-deferred and completed by the durable inbox
///     retry pipeline instead of holding the provider's connection open.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/billing/webhooks")]
[Microsoft.AspNetCore.Http.Tags("billing/webhooks")]
public sealed class BillingWebhooksController(
    ISender sender,
    IOptions<BillingConfiguration> billingConfiguration,
    ILogger<BillingWebhooksController> logger) : BaseApiController
{
    private readonly WebhookSettings _webhookSettings = billingConfiguration.Value.Webhook;
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
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Webhook)]
    [ServiceFilter(typeof(WebhookSourceSecurityFilter))]
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

            logger.LogInformation("Processing Google Pay webhook for project: {ProjectId}", LogRedaction.Sanitize(projectId));

            var result = await SendWithResponseBoundAsync(
                new ProcessGooglePayWebhookCommand(payload, authHeader, projectId), ct);

            return result is null
                ? DeferredAcknowledgement()
                : Ok(new { received = true, processed = result.Processed });
        }
        catch (InvalidWebhookSignatureException ex)
        {
            logger.LogWarning("Invalid Google Pay webhook signature: {Message}", LogRedaction.Sanitize(ex.Message));

            return Unauthorized(new { error = "Invalid signature" });
        }
    }

    /// <summary>
    ///     Handle Apple Pay webhook events
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Webhook processing confirmation</returns>
    /// <remarks>
    ///     Processes Apple Pay webhook notifications for payment processing and transaction updates.
    ///     Required Headers:
    ///     - Apple-Pay-Merchant-Id: Merchant identifier for validation
    ///     - Apple-Pay-Signature: Signature for webhook verification
    /// </remarks>
    [HttpPost("apple-pay")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Webhook)]
    [ServiceFilter(typeof(WebhookSourceSecurityFilter))]
    [EndpointSummary("Handle Apple Pay webhook events for transaction notifications")]
    [EndpointDescription("Processes Apple Pay webhook notifications for payment completions and transaction status updates.")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HandleApplePayWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);

        var merchantId = Request.Headers["Apple-Pay-Merchant-Id"].ToString();
        var signature = Request.Headers["Apple-Pay-Signature"].ToString();

        if (string.IsNullOrEmpty(merchantId))
        {
            logger.LogWarning("Missing Apple Pay merchant ID header");
            return BadRequest(new { error = "Missing merchant ID header" });
        }

        if (string.IsNullOrEmpty(signature))
        {
            logger.LogWarning("Missing Apple Pay signature header");
            return BadRequest(new { error = "Missing signature header" });
        }

        logger.LogInformation("Processing Apple Pay webhook for merchant: {MerchantId}", LogRedaction.Sanitize(merchantId));

        var result = await SendWithResponseBoundAsync(
            new ProcessApplePayWebhookCommand(payload, merchantId, signature), ct).ConfigureAwait(false);

        if (result is null)
        {
            return DeferredAcknowledgement();
        }

        if (result.Processed || result.WasAlreadyProcessed)
        {
            return Ok(new { received = true, processed = result.Processed, eventId = result.EventId });
        }

        return StatusCode(500, new { error = result.ErrorMessage, requiresRetry = result.RequiresRetry });
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
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Webhook)]
    [ServiceFilter(typeof(WebhookSourceSecurityFilter))]
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

            var result = await SendWithResponseBoundAsync(new ProcessStripeWebhookCommand(payload, signature), ct)
                .ConfigureAwait(false);

            if (result is null)
            {
                return DeferredAcknowledgement();
            }

            return result.Processed || result.WasAlreadyProcessed
                ? Ok(new { received = true, processed = result.Processed, eventId = result.EventId })
                : StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = result.ErrorMessage,
                    requiresRetry = result.RequiresRetry
                });
        }
        catch (InvalidWebhookSignatureException)
        {
            logger.LogWarning("Invalid Stripe webhook signature");

            return BadRequest(new { error = "Invalid signature" });
        }
        catch (InvalidWebhookPayloadException exception)
        {
            logger.LogWarning("Invalid Stripe webhook payload: {Message}", exception.Message);
            return BadRequest(new { error = "Invalid payload" });
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
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Webhook)]
    [ServiceFilter(typeof(WebhookSourceSecurityFilter))]
    [EndpointSummary("Handle PayPal IPN (Instant Payment Notification) webhook events")]
    [EndpointDescription(
        "Processes PayPal Instant Payment Notification (IPN) webhook events for subscription billing, payment confirmations, and account updates. PayPal IPN provides real-time transaction status updates and subscription lifecycle management for PayPal-based billing integrations."
    )]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HandlePayPalWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);

        // PayPal webhook requires specific headers for signature verification
        var transmissionId = Request.Headers["PayPal-Transmission-Id"].ToString();
        var transmissionTime = Request.Headers["PayPal-Transmission-Time"].ToString();
        var transmissionSig = Request.Headers["PayPal-Transmission-Sig"].ToString();
        var certUrl = Request.Headers["PayPal-Cert-Url"].ToString();
        var authAlgo = Request.Headers["PayPal-Auth-Algo"].ToString();

        if (string.IsNullOrEmpty(transmissionId) ||
            string.IsNullOrEmpty(transmissionTime) ||
            string.IsNullOrEmpty(transmissionSig))
        {
            logger.LogWarning("Missing required PayPal IPN headers");
            return BadRequest(new { error = "Missing required PayPal headers" });
        }

        var result = await SendWithResponseBoundAsync(new ProcessPayPalWebhookCommand(
                payload,
                transmissionId,
                transmissionSig,
                transmissionTime,
                string.IsNullOrEmpty(certUrl) ? null : certUrl,
                string.IsNullOrEmpty(authAlgo) ? null : authAlgo), ct)
            .ConfigureAwait(false);

        if (result is null)
        {
            return DeferredAcknowledgement();
        }

        if (result.Processed || result.WasAlreadyProcessed)
        {
            return Ok(new { received = true, processed = result.Processed, eventId = result.EventId });
        }

        return StatusCode(500, new { error = result.ErrorMessage, requiresRetry = result.RequiresRetry });
    }

    /// <summary>
    ///     Handle Braintree webhook events with offline signature verification
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Webhook processing confirmation</returns>
    /// <remarks>
    ///     Braintree delivers <c>application/x-www-form-urlencoded</c> bodies carrying the
    ///     <c>bt_signature</c>/<c>bt_payload</c> pair. The signature is verified offline against
    ///     the gateway's configured public key (issue #408) before any inbox work; failed
    ///     verification is rejected fail-closed with a security event. The stored payload is the
    ///     privacy-minimized routing projection of the notification.
    /// </remarks>
    [HttpPost("braintree")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Webhook)]
    [ServiceFilter(typeof(WebhookSourceSecurityFilter))]
    [EndpointSummary("Handle Braintree webhook events with offline signature verification")]
    [EndpointDescription(
        "Processes Braintree webhook notifications for subscription charges, failures and lifecycle events. The bt_signature form field is verified offline against the configured gateway public key before any inbox work; failed verification is rejected fail-closed."
    )]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleBraintreeWebhook(CancellationToken ct)
    {
        IFormCollection form;
        try
        {
            form = await Request.ReadFormAsync(ct);
        }
        catch (Exception exception) when (exception is InvalidOperationException or FileLoadException)
        {
            // A delivery without a parseable form body (wrong content type, rejected
            // multipart limits) is malformed, not unauthorized: reject it as a bad request.
            logger.LogWarning("Braintree webhook body could not be read as a form: {Message}", LogRedaction.Sanitize(exception.Message));
            return BadRequest(new { error = "Missing required Braintree form fields" });
        }

        var btSignature = form["bt_signature"].ToString();
        var btPayload = form["bt_payload"].ToString();

        if (string.IsNullOrEmpty(btSignature) || string.IsNullOrEmpty(btPayload))
        {
            logger.LogWarning("Missing Braintree bt_signature/bt_payload form fields");
            return BadRequest(new { error = "Missing required Braintree form fields" });
        }

        var result = await SendWithResponseBoundAsync(new ProcessBraintreeWebhookCommand(btSignature, btPayload), ct)
            .ConfigureAwait(false);

        if (result is null)
        {
            return DeferredAcknowledgement();
        }

        if (result.Processed || result.WasAlreadyProcessed)
        {
            return Ok(new { received = true, processed = result.Processed, eventId = result.EventId });
        }

        return StatusCode(500, new { error = result.ErrorMessage, requiresRetry = result.RequiresRetry });
    }

    /// <summary>
    ///     Executes a provider webhook command under the explicit response bound
    ///     (issue #408): a linked cancellation source cancels after
    ///     <see cref="WebhookSettings.ResponseBoundSeconds"/> (strictly under the 5-second
    ///     provider tolerance). When the bound fires the endpoint returns <c>null</c> and the
    ///     caller acknowledges the delivery as received-and-deferred — the durable inbox row
    ///     and the hosted retry worker complete the work. A client-side disconnect still
    ///     propagates as cancellation.
    /// </summary>
    private async Task<TResponse?> SendWithResponseBoundAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken ct)
    {
        using var boundedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        boundedCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _webhookSettings.ResponseBoundSeconds)));

        try
        {
            return await sender.Send(command, boundedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (boundedCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            logger.LogWarning(
                "Webhook processing exceeded the {BoundSeconds}s response bound; acknowledging as deferred (command {CommandType})",
                _webhookSettings.ResponseBoundSeconds,
                command.GetType().Name);
            return default;
        }
    }

    /// <summary>The acknowledgement returned when the response bound deferred processing (issue #408).</summary>
    private IActionResult DeferredAcknowledgement() =>
        Ok(new { received = true, processed = false, deferred = true });

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
    [HttpGet("webhook-events/{eventId}")]
    [Authorize(Policy = Policies.SystemAdmin)]
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
    [HttpPost("webhook-events/{eventId}:retry")]
    [Authorize(Policy = Policies.SystemAdmin)]
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

    /// <summary>
    ///     Get the billing webhook security monitoring summary
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Security posture summary for the billing webhook endpoints</returns>
    /// <remarks>
    ///     Admin-facing monitoring surface for the billing webhook security controls: the state of
    ///     the source IP allowlist, the suspicious-activity threshold blocking (currently blocked
    ///     sources), and the delivery health of the central security event pipeline that persists
    ///     verification failures, allowlist rejections, and replay detections.
    /// </remarks>
    [HttpGet("security")]
    [Authorize(Policy = Policies.SystemAdmin)]
    [EndpointSummary("Get the billing webhook security monitoring summary")]
    [EndpointDescription(
        "Admin-facing monitoring surface for the billing webhook security controls: the state of the source IP allowlist, the suspicious-activity threshold blocking (currently blocked sources), and the delivery health of the central security event pipeline that persists verification failures, allowlist rejections, and replay detections."
    )]
    [ProducesResponseType<BillingWebhookSecuritySummaryDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhookSecuritySummary(CancellationToken ct)
    {
        var summary = await sender.Send(new GetBillingWebhookSecuritySummaryQuery(), ct).ConfigureAwait(false);

        return Ok(summary);
    }

    /// <summary>
    ///     Get the billing webhook processing statistics (issue #408)
    /// </summary>
    /// <param name="fromUtc">Optional inclusive window start (ISO-8601); defaults to 24 hours ago.</param>
    /// <param name="toUtc">Optional inclusive window end (ISO-8601); defaults to now.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Webhook activity statistics for the window</returns>
    /// <remarks>
    ///     Admin-facing webhook activity dashboard (issue #408): events received, processed and
    ///     failed in the window, the processing success rate, the average received-to-processed
    ///     latency, the dead-lettered and unverified counts, and a per-provider breakdown.
    /// </remarks>
    [HttpGet("stats")]
    [Authorize(Policy = Policies.SystemAdmin)]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [EndpointSummary("Get billing webhook processing statistics")]
    [EndpointDescription(
        "Admin-facing webhook activity dashboard: events received, processed and currently failed, the processing success rate, the average received-to-processed latency in seconds, dead-lettered and unverified counts over the window, and a per-provider breakdown. Defaults to the last 24 hours. Requires the SystemAdmin policy."
    )]
    [ProducesResponseType<BillingWebhookStatsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWebhookStats(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var stats = await sender.Send(new GetBillingWebhookStatsQuery(fromUtc, toUtc), ct).ConfigureAwait(false);

        return Ok(stats);
    }

    /// <summary>
    ///     Rotate a provider's webhook signing secret (issue #408)
    /// </summary>
    /// <param name="body">Rotation request (provider key and the new secret exactly as configured at the provider).</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Rotation record: when the previous secret stops being accepted</returns>
    /// <remarks>
    ///     Records a signing-secret rotation for one provider webhook endpoint. During the
    ///     rotation window (Billing:Webhook:SecretRotationWindowHours, default 24h) deliveries
    ///     signed with either the previous or the new secret verify (dual-secret verification);
    ///     after the window the previous secret is automatically deprecated and rejected. The
    ///     new secret is stored encrypted at rest and never appears in logs or the response.
    ///     Requires the SystemAdmin policy; the acting administrator is taken from the
    ///     authenticated actor, and the decision is audited.
    /// </remarks>
    [HttpPost("secrets:rotate")]
    [Authorize(Policy = Policies.SystemAdmin)]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [EndpointSummary("Rotate a provider webhook signing secret")]
    [EndpointDescription(
        "Records a webhook signing-secret rotation for a provider. During the rotation window both the previous and the new secret verify (dual-secret verification); after the window the previous secret is automatically deprecated and rejected. Secrets are stored encrypted at rest. Requires the SystemAdmin policy."
    )]
    [ProducesResponseType<WebhookSecretRotationResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RotateSecret([FromBody] RotateWebhookSecretRequest body, CancellationToken ct)
    {
        try
        {
            var result = await sender.Send(
                new RotateWebhookSecretCommand(body.ProviderKey, body.NewSecret), ct).ConfigureAwait(false);

            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    /// <summary>Request body for the webhook signing-secret rotation.</summary>
    /// <param name="ProviderKey">Provider key (stripe, paypal, ...).</param>
    /// <param name="NewSecret">The new signing secret exactly as configured at the provider.</param>
    public sealed record RotateWebhookSecretRequest(string ProviderKey, string NewSecret);
}
