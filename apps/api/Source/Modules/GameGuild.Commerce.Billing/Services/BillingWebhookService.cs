using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Service for handling billing webhooks from external providers
/// </summary>
public abstract class BillingWebhookService(ILogger<BillingWebhookService> logger) : IBillingWebhookService
{
    // TODO: Inject ISubscriptionService when Subscriptions module integration is complete
    // private readonly ISubscriptionService _subscriptionService;

    /// <inheritdoc />
    public Task HandleSubscriptionCreatedAsync(SubscriptionWebhookPayload payload)
    {
        try
        {
            logger.LogInformation("Handling subscription created webhook for tenant {TenantId}, subscription {SubscriptionId}", payload.TenantId, payload.ExternalSubscriptionId);

            // TODO: Integrate with Subscriptions module
            // await _subscriptionService.CreateSubscriptionAsync(new CreateSubscriptionCommand(
            //     payload.TenantId,
            //     payload.PlanId,
            //     payload.ExternalSubscriptionId,
            //     payload.Status,
            //     payload.Amount,
            //     payload.StartDate,
            //     payload.EndDate,
            //     payload.NextBillingDate
            // ));

            logger.LogInformation("Successfully processed subscription created webhook");

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling subscription created webhook");

            throw;
        }
    }

    /// <inheritdoc />
    public Task HandleSubscriptionUpdatedAsync(SubscriptionWebhookPayload payload)
    {
        try
        {
            logger.LogInformation("Handling subscription updated webhook for tenant {TenantId}, subscription {SubscriptionId}", payload.TenantId, payload.ExternalSubscriptionId);

            // TODO: Integrate with Subscriptions module
            // await _subscriptionService.UpdateSubscriptionAsync(new UpdateSubscriptionCommand(
            //     payload.TenantId,
            //     payload.ExternalSubscriptionId,
            //     payload.Status,
            //     payload.Amount,
            //     payload.EndDate,
            //     payload.NextBillingDate
            // ));

            logger.LogInformation("Successfully processed subscription updated webhook");

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling subscription updated webhook");

            throw;
        }
    }

    /// <inheritdoc />
    public Task HandleSubscriptionCanceledAsync(SubscriptionWebhookPayload payload)
    {
        try
        {
            logger.LogInformation("Handling subscription canceled webhook for tenant {TenantId}, subscription {SubscriptionId}", payload.TenantId, payload.ExternalSubscriptionId);

            // TODO: Integrate with Subscriptions module
            // await _subscriptionService.CancelSubscriptionAsync(new CancelSubscriptionCommand(
            //     payload.TenantId,
            //     payload.ExternalSubscriptionId
            // ));

            logger.LogInformation("Successfully processed subscription canceled webhook");

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling subscription canceled webhook");

            throw;
        }
    }

    /// <inheritdoc />
    public Task HandlePaymentSucceededAsync(PaymentWebhookPayload payload)
    {
        try
        {
            logger.LogInformation("Handling payment succeeded webhook for tenant {TenantId}, payment {PaymentId}", payload.TenantId, payload.PaymentId);

            // TODO: Integrate with Subscriptions module to record payment
            // await _subscriptionService.RecordPaymentAsync(new RecordPaymentCommand(
            //     payload.TenantId,
            //     payload.ExternalSubscriptionId,
            //     payload.PaymentId,
            //     payload.Amount,
            //     payload.Currency,
            //     payload.PaidAt,
            //     payload.Status,
            //     payload.Metadata
            // ));

            logger.LogInformation("Successfully processed payment succeeded webhook");

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling payment succeeded webhook");

            throw;
        }
    }

    /// <inheritdoc />
    public Task HandlePaymentFailedAsync(PaymentWebhookPayload payload)
    {
        try
        {
            logger.LogInformation("Handling payment failed webhook for tenant {TenantId}, payment {PaymentId}", payload.TenantId, payload.PaymentId);

            // TODO: Integrate with Subscriptions module to record payment failure
            // await _subscriptionService.RecordPaymentFailureAsync(new RecordPaymentFailureCommand(
            //     payload.TenantId,
            //     payload.ExternalSubscriptionId,
            //     payload.PaymentId,
            //     payload.Amount,
            //     payload.Currency,
            //     payload.FailureReason,
            //     payload.Metadata
            // ));

            logger.LogInformation("Successfully processed payment failed webhook");

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling payment failed webhook");

            throw;
        }
    }
}
