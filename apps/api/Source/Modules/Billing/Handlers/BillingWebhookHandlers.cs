using GameGuild.CQRS;
using GameGuild.Modules.Billing.Commands;
using GameGuild.Modules.Billing.Models;
using GameGuild.Modules.Billing.Services;


namespace GameGuild.Modules.Billing.Handlers;

/// <summary> Handler for processing billing webhooks </summary>
public class ProcessBillingWebhookHandler : IRequestHandler<ProcessBillingWebhookCommand, WebhookProcessingResult> {
  private readonly ILogger<ProcessBillingWebhookHandler> _logger;

  private readonly IBillingWebhookService _webhookService;

  public ProcessBillingWebhookHandler(ILogger<ProcessBillingWebhookHandler> logger, IBillingWebhookService webhookService) {
    _logger = logger;
    _webhookService = webhookService;
  }

  public async Task<WebhookProcessingResult> Handle(ProcessBillingWebhookCommand command, CancellationToken cancellationToken) {
    try {
      _logger.LogInformation("Processing {Provider} webhook", command.Provider);

      var result = await _webhookService.ProcessWebhookAsync(command.Provider, command.Payload, command.Headers, cancellationToken);

      if (result.IsSuccess) { _logger.LogInformation("Successfully processed {Provider} webhook", command.Provider); }
      else { _logger.LogWarning("Failed to process {Provider} webhook: {Error}", command.Provider, result.ErrorMessage); }

      return result;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error processing {Provider} webhook", command.Provider);

      return WebhookProcessingResult.Failure($"Unexpected error: {ex.Message}");
    }
  }
}

/// <summary> Handler for processing Stripe webhooks </summary>
public class ProcessStripeWebhookHandler : IRequestHandler<ProcessStripeWebhookCommand, WebhookProcessingResult> {
  private readonly ILogger<ProcessStripeWebhookHandler> _logger;

  private readonly IBillingWebhookService _webhookService;

  public ProcessStripeWebhookHandler(ILogger<ProcessStripeWebhookHandler> logger, IBillingWebhookService webhookService) {
    _logger = logger;
    _webhookService = webhookService;
  }

  public async Task<WebhookProcessingResult> Handle(ProcessStripeWebhookCommand command, CancellationToken cancellationToken) {
    try {
      _logger.LogInformation("Processing Stripe webhook");

      var result = await _webhookService.ProcessStripeWebhookAsync(command.Payload, command.SignatureHeader, cancellationToken);

      return result;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error processing Stripe webhook");

      return WebhookProcessingResult.Failure($"Stripe webhook processing failed: {ex.Message}");
    }
  }
}

/// <summary> Handler for processing PayPal webhooks </summary>
public class ProcessPayPalWebhookHandler : IRequestHandler<ProcessPayPalWebhookCommand, WebhookProcessingResult> {
  private readonly ILogger<ProcessPayPalWebhookHandler> _logger;

  private readonly IBillingWebhookService _webhookService;

  public ProcessPayPalWebhookHandler(ILogger<ProcessPayPalWebhookHandler> logger, IBillingWebhookService webhookService) {
    _logger = logger;
    _webhookService = webhookService;
  }

  public async Task<WebhookProcessingResult> Handle(ProcessPayPalWebhookCommand command, CancellationToken cancellationToken) {
    try {
      _logger.LogInformation("Processing PayPal webhook");

      var result = await _webhookService.ProcessPayPalWebhookAsync(command.Payload, command.Headers, cancellationToken);

      return result;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error processing PayPal webhook");

      return WebhookProcessingResult.Failure($"PayPal webhook processing failed: {ex.Message}");
    }
  }
}
