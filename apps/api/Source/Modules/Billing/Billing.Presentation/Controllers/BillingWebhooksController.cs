using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GameGuild.Modules.Billing.Features.ProcessWebhook;
using GameGuild.Modules.Billing.Features.ManageWebhook;
using GameGuild.Modules.Billing.Features.GetWebhook;
using GameGuild.Modules.Billing.Models;
using GameGuild.Modules.Billing.Exceptions;
using MediatR;

namespace GameGuild.Modules.Billing.Controllers;

/// <summary>
///     Controller for handling billing webhooks from external payment providers
/// </summary>
[ApiController]
[Route("api/webhooks/billing")]
public sealed class BillingWebhooksController : ControllerBase
{
    private readonly ILogger<BillingWebhooksController> _logger;

    private readonly ISender _sender;

    public BillingWebhooksController(ISender sender, ILogger<BillingWebhooksController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    /// <summary>
    ///     Generic webhook endpoint for billing events
    /// </summary>
    /// <param name="provider">Payment provider (stripe, paypal, etc.)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Webhook processing result</returns>
    [HttpPost("{provider}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> HandleWebhook(string provider, CancellationToken ct)
    {
        try
        {
            // Read the raw body
            using var reader = new StreamReader(Request.Body);
            string payload = await reader.ReadToEndAsync(ct);

            // Get headers for signature verification
            var headers = new Dictionary<string, string>();
            foreach (var header in Request.Headers)
            {
                headers[header.Key] = header.Value.ToString();
            }

            _logger.LogInformation("Received {Provider} webhook with payload length: {Length}", provider, payload.Length);

            var result = await _sender.Send(
                new ProcessBillingWebhookCommand(
                    provider,
                    payload,
                    headers
                ),
                ct
            ).ConfigureAwait(false);

            return Ok(
                new
                {
                    success = true, processed = result.Processed, eventId = result.EventId
                }
            );
        }
        catch (InvalidWebhookSignatureException ex)
        {
            _logger.LogWarning("Invalid webhook signature from {Provider}: {Message}", provider, ex.Message);

            return BadRequest(
                new
                {
                    error = "Invalid signature"
                }
            );
        }
        catch (UnsupportedWebhookEventException ex)
        {
            _logger.LogInformation("Unsupported webhook event from {Provider}: {EventType}", provider, ex.EventType);

            return Ok(
                new
                {
                    success = true, processed = false, reason = "Unsupported event type"
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Provider} webhook", provider);

            return UnprocessableEntity(
                new
                {
                    error = "Webhook processing failed"
                }
            );
        }
    }

    /// <summary>
    ///     Stripe-specific webhook endpoint with signature verification
    /// </summary>
    [HttpPost("stripe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleStripeWebhook(CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            string payload = await reader.ReadToEndAsync(ct);

            var signature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Missing Stripe signature header");

                return BadRequest(
                    new
                    {
                        error = "Missing signature"
                    }
                );
            }

            var headers = new Dictionary<string, string>
            {
                ["Stripe-Signature"] = signature
            };

            var result = await _sender.Send(new ProcessStripeWebhookCommand(payload, signature), ct).ConfigureAwait(false);

            return Ok(
                new
                {
                    received = true
                }
            );
        }
        catch (InvalidWebhookSignatureException)
        {
            _logger.LogWarning("Invalid Stripe webhook signature");

            return BadRequest(
                new
                {
                    error = "Invalid signature"
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");

            return StatusCode(500);
        }
    }

    /// <summary>
    ///     PayPal IPN webhook endpoint
    /// </summary>
    [HttpPost("paypal")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HandlePayPalWebhook(CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            string payload = await reader.ReadToEndAsync(ct);

            var result = await _sender.Send(new ProcessPayPalWebhookCommand(payload), ct).ConfigureAwait(false);

            return Ok(
                new
                {
                    received = true
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal webhook");

            return StatusCode(500);
        }
    }

    /// <summary>
    ///     Webhook event status endpoint for debugging
    /// </summary>
    [HttpGet("events/{eventId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWebhookEvent(string eventId, CancellationToken ct)
    {
        var webhookEvent = await _sender.Send(new GetWebhookEventQuery(eventId), ct).ConfigureAwait(false);

        return webhookEvent is null ? NotFound() : Ok(webhookEvent);
    }

    /// <summary>
    ///     Retry failed webhook processing
    /// </summary>
    [HttpPost("events/{eventId}/retry")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryWebhookEvent(string eventId, CancellationToken ct)
    {
        var result = await _sender.Send(new RetryWebhookEventCommand(eventId), ct).ConfigureAwait(false);

        return result.Success ? Ok(result) : NotFound();
    }
}

