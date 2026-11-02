using GameGuild.Commerce.Payments;
using GameGuild.Commerce.Subscriptions;
using GameGuild.CQRS;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;

namespace GameGuild.API.Integration;

/// <summary>
///     Cross-module event handler that emails the customer a confirmation when a payment
///     method is added to their account (issue #414).
/// </summary>
/// <remarks>
///     Sends an Email-channel Billing notification to the payment-method owner. Resides in
///     the API composition root to keep the Commerce.Payments and Notifications modules
///     independent (mirrors <see cref="SubscriptionPaymentFailedEmailHandler"/>).
/// </remarks>
public sealed class PaymentMethodRegisteredEmailHandler(
    INotificationService notificationService,
    IMonthlyStatementLinkBuilder statementLinkBuilder,
    ILogger<PaymentMethodRegisteredEmailHandler> logger
) : INotificationHandler<PaymentMethodRegisteredEvent>
{
    public async Task Handle(PaymentMethodRegisteredEvent notification, CancellationToken cancellationToken)
    {
        var methodLabel = DescribeMethod(notification.CardBrand, notification.CardLast4);

        try
        {
            var result = await notificationService.SendAsync(
                recipientId: notification.UserId,
                type: NotificationType.Billing,
                title: notification.Restored ? "Payment method re-added" : "Payment method added",
                message:
                    $"Your {methodLabel} was {(notification.Restored ? "re-added" : "added")} to your account" +
                    (notification.Restored ? string.Empty : " and is available for subscriptions and purchases") +
                    ". If you did not make this change, contact support immediately.",
                channel: NotificationChannel.Email,
                tenantId: notification.TenantId,
                actionUrl: statementLinkBuilder.GetBillingDashboardPath(),
                priority: NotificationPriority.Normal,
                referenceEntityId: notification.PaymentMethodId,
                referenceEntityType: nameof(PaymentMethod),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Payment-method added confirmation email queued for user {UserId} (payment method {PaymentMethodId})",
                    notification.UserId, notification.PaymentMethodId);
            }
            else
            {
                logger.LogInformation(
                    "Payment-method added confirmation for payment method {PaymentMethodId} was not delivered: {Reason}",
                    notification.PaymentMethodId, result.Error?.Description ?? "preference decision");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Best-effort confirmation: delivery problems never fail the committed registration.
            logger.LogWarning(
                exception,
                "Failed to queue the payment-method added confirmation for payment method {PaymentMethodId}",
                notification.PaymentMethodId);
        }
    }

    internal static string DescribeMethod(string? cardBrand, string? cardLast4)
    {
        var brand = string.IsNullOrWhiteSpace(cardBrand) ? "payment method" : $"{cardBrand} card";
        return cardLast4 is null ? brand : $"{brand} ending in {cardLast4}";
    }
}

/// <summary>
///     Cross-module event handler that emails the customer a confirmation when a payment
///     method is removed from their account (issue #414).
/// </summary>
/// <remarks>
///     Sends an Email-channel Billing notification to the payment-method owner. Resides in
///     the API composition root to keep the Commerce.Payments and Notifications modules
///     independent (mirrors <see cref="SubscriptionPaymentFailedEmailHandler"/>).
/// </remarks>
public sealed class PaymentMethodDeletedEmailHandler(
    INotificationService notificationService,
    IMonthlyStatementLinkBuilder statementLinkBuilder,
    ILogger<PaymentMethodDeletedEmailHandler> logger
) : INotificationHandler<PaymentMethodDeletedEvent>
{
    public async Task Handle(PaymentMethodDeletedEvent notification, CancellationToken cancellationToken)
    {
        var methodLabel = PaymentMethodRegisteredEmailHandler.DescribeMethod(notification.CardBrand, notification.CardLast4);

        try
        {
            var result = await notificationService.SendAsync(
                recipientId: notification.UserId,
                type: NotificationType.Billing,
                title: "Payment method removed",
                message:
                    $"Your {methodLabel} was removed from your account. " +
                    "If you did not make this change, contact support immediately.",
                channel: NotificationChannel.Email,
                tenantId: notification.TenantId,
                actionUrl: statementLinkBuilder.GetBillingDashboardPath(),
                priority: NotificationPriority.Normal,
                referenceEntityId: notification.PaymentMethodId,
                referenceEntityType: nameof(PaymentMethod),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Payment-method removed confirmation email queued for user {UserId} (payment method {PaymentMethodId})",
                    notification.UserId, notification.PaymentMethodId);
            }
            else
            {
                logger.LogInformation(
                    "Payment-method removed confirmation for payment method {PaymentMethodId} was not delivered: {Reason}",
                    notification.PaymentMethodId, result.Error?.Description ?? "preference decision");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Best-effort confirmation: delivery problems never fail the committed removal.
            logger.LogWarning(
                exception,
                "Failed to queue the payment-method removed confirmation for payment method {PaymentMethodId}",
                notification.PaymentMethodId);
        }
    }
}
