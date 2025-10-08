using GameGuild.Modules.Billing.Models;

namespace GameGuild.Modules.Billing.Abstractions;

/// <summary>
/// Interface for handling billing webhooks from external providers
/// </summary>
public interface IBillingWebhookService
{
    /// <summary>
    /// Handle subscription created webhook
    /// </summary>
    Task HandleSubscriptionCreatedAsync(SubscriptionWebhookPayload payload);
    
    /// <summary>
    /// Handle subscription updated webhook
    /// </summary>
    Task HandleSubscriptionUpdatedAsync(SubscriptionWebhookPayload payload);
    
    /// <summary>
    /// Handle subscription canceled webhook
    /// </summary>
    Task HandleSubscriptionCanceledAsync(SubscriptionWebhookPayload payload);
    
    /// <summary>
    /// Handle payment succeeded webhook
    /// </summary>
    Task HandlePaymentSucceededAsync(PaymentWebhookPayload payload);
    
    /// <summary>
    /// Handle payment failed webhook
    /// </summary>
    Task HandlePaymentFailedAsync(PaymentWebhookPayload payload);
}

