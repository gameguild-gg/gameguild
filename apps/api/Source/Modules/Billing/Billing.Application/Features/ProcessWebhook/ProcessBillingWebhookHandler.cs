using System.Text.Json;
using Microsoft.Extensions.Logging;
using GameGuild.Modules.Billing.Entities;
using GameGuild.Modules.Billing.Models;
using GameGuild.Modules.Billing.Exceptions;
using MediatR;

namespace GameGuild.Modules.Billing.Features.ProcessWebhook;

/// <summary>
///     Handler for processing billing webhooks
/// </summary>
public class ProcessBillingWebhookHandler : ICommandHandler<ProcessBillingWebhookCommand, WebhookProcessingResult>
{
    private readonly ILogger<ProcessBillingWebhookHandler> _logger;
    // Note: You'll need to inject repositories and services as needed

    public ProcessBillingWebhookHandler(ILogger<ProcessBillingWebhookHandler> logger)
    {
        _logger = logger;
    }

    public async Task<WebhookProcessingResult> Handle(ProcessBillingWebhookCommand command, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing {Provider} webhook", command.Provider);

            // Create webhook event record
            var webhookEvent = new BillingWebhookEvent
            {
                Provider = command.Provider,
                Payload = command.Payload,
                Headers = JsonSerializer.Serialize(command.Headers),
                ExternalEventId = ExtractEventId(command.Payload, command.Provider),
                EventType = ExtractEventType(command.Payload, command.Provider)
            };

            // TODO: Save webhook event to repository
            // await _webhookRepository.CreateAsync(webhookEvent, cancellationToken);

            // Process based on provider
            bool processed = command.Provider.ToLowerInvariant() switch
            {
                "stripe" => await ProcessStripeEvent(webhookEvent, cancellationToken),
                "paypal" => await ProcessPayPalEvent(webhookEvent, cancellationToken),
                _ => throw new UnsupportedWebhookEventException(command.Provider)
            };

            if (processed)
            {
                webhookEvent.MarkAsProcessed();
                // TODO: Update webhook event in repository
            }

            return new WebhookProcessingResult
            {
                Processed = processed,
                EventId = webhookEvent.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Provider} webhook", command.Provider);
            throw;
        }
    }

    private async Task<bool> ProcessStripeEvent(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        return webhookEvent.EventType switch
        {
            "customer.subscription.created" => await HandleSubscriptionCreated(webhookEvent, cancellationToken),
            "customer.subscription.updated" => await HandleSubscriptionUpdated(webhookEvent, cancellationToken),
            "customer.subscription.deleted" => await HandleSubscriptionCancelled(webhookEvent, cancellationToken),
            "invoice.payment_succeeded" => await HandlePaymentSucceeded(webhookEvent, cancellationToken),
            "invoice.payment_failed" => await HandlePaymentFailed(webhookEvent, cancellationToken),
            _ => false // Unsupported event type, but not an error
        };
    }

    private async Task<bool> ProcessPayPalEvent(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        return webhookEvent.EventType switch
        {
            "BILLING.SUBSCRIPTION.CREATED" => await HandleSubscriptionCreated(webhookEvent, cancellationToken),
            "BILLING.SUBSCRIPTION.UPDATED" => await HandleSubscriptionUpdated(webhookEvent, cancellationToken),
            "BILLING.SUBSCRIPTION.CANCELLED" => await HandleSubscriptionCancelled(webhookEvent, cancellationToken),
            "PAYMENT.SALE.COMPLETED" => await HandlePaymentSucceeded(webhookEvent, cancellationToken),
            "PAYMENT.SALE.DENIED" => await HandlePaymentFailed(webhookEvent, cancellationToken),
            _ => false
        };
    }

    private async Task<bool> HandleSubscriptionCreated(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling subscription created event for {Provider}", webhookEvent.Provider);
        // TODO: Implement subscription creation logic
        return true;
    }

    private async Task<bool> HandleSubscriptionUpdated(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling subscription updated event for {Provider}", webhookEvent.Provider);
        // TODO: Implement subscription update logic
        return true;
    }

    private async Task<bool> HandleSubscriptionCancelled(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling subscription cancelled event for {Provider}", webhookEvent.Provider);
        // TODO: Implement subscription cancellation logic
        return true;
    }

    private async Task<bool> HandlePaymentSucceeded(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling payment succeeded event for {Provider}", webhookEvent.Provider);
        // TODO: Implement payment success logic
        return true;
    }

    private async Task<bool> HandlePaymentFailed(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling payment failed event for {Provider}", webhookEvent.Provider);
        // TODO: Implement payment failure logic
        return true;
    }

    private string ExtractEventId(string payload, string provider)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonDocument>(payload);

            return provider.ToLowerInvariant() switch
            {
                "stripe" => json?.RootElement.GetProperty("id").GetString() ?? Guid.NewGuid().ToString(),
                "paypal" => json?.RootElement.GetProperty("id").GetString() ?? Guid.NewGuid().ToString(),
                _ => Guid.NewGuid().ToString()
            };
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }

    private string ExtractEventType(string payload, string provider)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonDocument>(payload);

            return provider.ToLowerInvariant() switch
            {
                "stripe" => json?.RootElement.GetProperty("type").GetString() ?? "unknown",
                "paypal" => json?.RootElement.GetProperty("event_type").GetString() ?? "unknown",
                _ => "unknown"
            };
        }
        catch
        {
            return "unknown";
        }
    }
}

