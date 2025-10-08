using System.Text.Json;
using Microsoft.Extensions.Logging;
using GameGuild.Modules.Billing.Entities;
using GameGuild.Modules.Billing.Models;
using GameGuild.Modules.Billing.Exceptions;
using MediatR;

namespace GameGuild.Modules.Billing.Features.ProcessWebhook;

/// <summary>
///     Handler for processing PayPal webhooks
/// </summary>
public class ProcessPayPalWebhookHandler : ICommandHandler<ProcessPayPalWebhookCommand, WebhookProcessingResult>
{
    private readonly ILogger<ProcessPayPalWebhookHandler> _logger;

    public ProcessPayPalWebhookHandler(ILogger<ProcessPayPalWebhookHandler> logger)
    {
        _logger = logger;
    }

    public async Task<WebhookProcessingResult> Handle(ProcessPayPalWebhookCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // Parse PayPal event
            var paypalEvent = JsonSerializer.Deserialize<JsonDocument>(command.Payload);
            string? eventType = paypalEvent?.RootElement.GetProperty("event_type").GetString();

            if (string.IsNullOrEmpty(eventType))
            {
                throw new UnsupportedWebhookEventException("Unknown");
            }

            // Create webhook event record
            var webhookEvent = new BillingWebhookEvent
            {
                Provider = "paypal",
                Payload = command.Payload,
                ExternalEventId = paypalEvent.RootElement.GetProperty("id").GetString() ?? Guid.NewGuid().ToString(),
                EventType = eventType
            };

            // Process PayPal-specific events
            bool processed = await ProcessPayPalEvent(webhookEvent, cancellationToken);

            if (processed)
            {
                webhookEvent.MarkAsProcessed();
            }

            return new WebhookProcessingResult
            {
                Processed = processed,
                EventId = webhookEvent.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal webhook");
            throw;
        }
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
        return await Task.FromResult(true);
    }

    private async Task<bool> HandleSubscriptionUpdated(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling subscription updated event for {Provider}", webhookEvent.Provider);
        // TODO: Implement subscription update logic
        return await Task.FromResult(true);
    }

    private async Task<bool> HandleSubscriptionCancelled(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling subscription cancelled event for {Provider}", webhookEvent.Provider);
        // TODO: Implement subscription cancellation logic
        return await Task.FromResult(true);
    }

    private async Task<bool> HandlePaymentSucceeded(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling payment succeeded event for {Provider}", webhookEvent.Provider);
        // TODO: Implement payment success logic
        return await Task.FromResult(true);
    }

    private async Task<bool> HandlePaymentFailed(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling payment failed event for {Provider}", webhookEvent.Provider);
        // TODO: Implement payment failure logic
        return await Task.FromResult(true);
    }
}

