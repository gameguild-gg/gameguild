using GameGuild.CQRS;
using GameGuild.Modules.Billing.Commands;
using GameGuild.Modules.Billing.Services;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.Billing.Controllers;

/// <summary> Controller for handling billing webhooks from external payment providers </summary>
[ApiController]
[Route("api/webhooks/billing")]
public sealed class BillingWebhooksController : ControllerBase {
  private readonly ILogger<BillingWebhooksController> _logger;

  private readonly IMediator _mediator;

  private readonly IBillingWebhookService _webhookService;

  public BillingWebhooksController(IMediator mediator, IBillingWebhookService webhookService, ILogger<BillingWebhooksController> logger) {
    _mediator = mediator;
    _webhookService = webhookService;
    _logger = logger;
  }

  /// <summary> Generic webhook endpoint for billing events </summary>
  /// <param name="provider"> Payment provider (stripe, paypal, etc.) </param>
  /// <param name="ct"> Cancellation token </param>
  /// <returns> Webhook processing result </returns>
  [HttpPost("{provider}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
  public async Task<IActionResult> HandleWebhook(string provider, CancellationToken ct) {
    try {
      // Read the raw body
      using var reader = new StreamReader(Request.Body);
      var payload = await reader.ReadToEndAsync(ct);

      if (string.IsNullOrEmpty(payload)) {
        _logger.LogWarning("Received empty payload from {Provider}", provider);

        return BadRequest(new { error = "Empty payload" });
      }

      // Get headers for signature verification
      var headers = new Dictionary<string, string>();

      foreach (var header in Request.Headers) { headers[header.Key] = header.Value.ToString(); }

      _logger.LogInformation("Received {Provider} webhook with payload length: {Length}", provider, payload.Length);

      var result = await _mediator.Send(new ProcessBillingWebhookCommand(provider, payload, headers), ct);

      if (result.IsSuccess) { return Ok(new { success = true, message = "Webhook processed successfully" }); }

      _logger.LogWarning("Webhook processing failed for {Provider}: {Error}", provider, result.ErrorMessage);

      return UnprocessableEntity(new { error = result.ErrorMessage });
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error processing webhook from {Provider}", provider);

      return StatusCode(500, new { error = "Internal server error" });
    }
  }

  /// <summary> Stripe-specific webhook endpoint with signature verification </summary>
  /// <param name="ct"> Cancellation token </param>
  /// <returns> Webhook processing result </returns>
  [HttpPost("stripe")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
  public async Task<IActionResult> HandleStripeWebhook(CancellationToken ct) {
    try {
      // Read the raw body
      using var reader = new StreamReader(Request.Body);
      var payload = await reader.ReadToEndAsync(ct);

      // Get the signature header
      var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

      if (string.IsNullOrEmpty(signatureHeader)) {
        _logger.LogWarning("Missing Stripe signature header");

        return BadRequest(new { error = "Missing signature header" });
      }

      _logger.LogInformation("Received Stripe webhook with payload length: {Length}", payload.Length);

      var result = await _mediator.Send(new ProcessStripeWebhookCommand(payload, signatureHeader), ct);

      if (result.IsSuccess) { return Ok(new { success = true, message = "Stripe webhook processed successfully" }); }

      return UnprocessableEntity(new { error = result.ErrorMessage });
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error processing Stripe webhook");

      return StatusCode(500, new { error = "Internal server error" });
    }
  }

  /// <summary> PayPal-specific webhook endpoint </summary>
  /// <param name="ct"> Cancellation token </param>
  /// <returns> Webhook processing result </returns>
  [HttpPost("paypal")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
  public async Task<IActionResult> HandlePayPalWebhook(CancellationToken ct) {
    try {
      // Read the raw body
      using var reader = new StreamReader(Request.Body);
      var payload = await reader.ReadToEndAsync(ct);

      // Get headers for verification
      var headers = new Dictionary<string, string>();

      foreach (var header in Request.Headers.Where(h => h.Key.StartsWith("PAYPAL-", StringComparison.OrdinalIgnoreCase))) { headers[header.Key] = header.Value.ToString(); }

      _logger.LogInformation("Received PayPal webhook with payload length: {Length}", payload.Length);

      var result = await _mediator.Send(new ProcessPayPalWebhookCommand(payload, headers), ct);

      if (result.IsSuccess) { return Ok(new { success = true, message = "PayPal webhook processed successfully" }); }

      return UnprocessableEntity(new { error = result.ErrorMessage });
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error processing PayPal webhook");

      return StatusCode(500, new { error = "Internal server error" });
    }
  }

  /// <summary> Get webhook events for debugging and monitoring </summary>
  /// <param name="tenantId"> Optional tenant ID filter </param>
  /// <param name="subscriptionId"> Optional subscription ID filter </param>
  /// <param name="userId"> Optional user ID filter </param>
  /// <param name="ct"> Cancellation token </param>
  /// <returns> List of webhook events </returns>
  [HttpGet("events")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<IActionResult> GetWebhookEvents([FromQuery] Guid? tenantId = null, [FromQuery] Guid? subscriptionId = null, [FromQuery] Guid? userId = null, CancellationToken ct = default) {
    try {
      var events = await _webhookService.GetWebhookEventsAsync(tenantId, subscriptionId, userId, ct);

      return Ok(
        new {
          events = events.Select(e => new {
                                   e.Id,
                                   e.Provider,
                                   e.EventType,
                                   e.IsProcessed,
                                   e.IsFailed,
                                   e.ProcessingAttempts,
                                   e.ProcessedAt,
                                   e.CreatedAt,
                                   e.TenantId,
                                   e.SubscriptionId,
                                   e.UserId,
                                   e.ErrorMessage,
                                 }
          ),
        }
      );
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error retrieving webhook events");

      return StatusCode(500, new { error = "Internal server error" });
    }
  }

  /// <summary> Retry processing a failed webhook </summary>
  /// <param name="webhookEventId"> Webhook event ID </param>
  /// <param name="ct"> Cancellation token </param>
  /// <returns> Retry result </returns>
  [HttpPost("events/{webhookEventId:guid}/retry")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> RetryWebhook(Guid webhookEventId, CancellationToken ct) {
    try {
      var result = await _webhookService.RetryWebhookProcessingAsync(webhookEventId, ct);

      if (result.IsSuccess) { return Ok(new { success = true, message = "Webhook retry successful" }); }

      return BadRequest(new { error = result.ErrorMessage });
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error retrying webhook {WebhookEventId}", webhookEventId);

      return StatusCode(500, new { error = "Internal server error" });
    }
  }
}
