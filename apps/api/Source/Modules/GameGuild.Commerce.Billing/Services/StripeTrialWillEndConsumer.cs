using System.Text.Json;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Sends the trial-ending reminder for <c>customer.subscription.trial_will_end</c> Stripe
///     webhooks (issue #408: "Trial ending → Send reminder email"). Runs ahead of the
///     subscription/invoice routing in <c>StripeBillingWebhookService</c> through the
///     <see cref="IStripeVerifiedEventConsumer"/> extension point, so the reminder flows from
///     the same verified, idempotent inbox as every other Stripe subscription event. The
///     subscription is resolved through its verified provider identity; the reminder email is
///     queued through the Notifications module (best-effort, like the invoice-issued email).
/// </summary>
public sealed class StripeTrialWillEndConsumer(
    ISubscriptionQueryService subscriptionQueryService,
    IUserRepository userRepository,
    INotificationService notificationService,
    ILogger<StripeTrialWillEndConsumer> logger) : IStripeVerifiedEventConsumer
{
    private const string TrialWillEndEventType = "customer.subscription.trial_will_end";

    private static readonly JsonSerializerOptions MetadataOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<bool> TryConsumeAsync(
        VerifiedStripeWebhookEvent verifiedEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(verifiedEvent);

        if (!string.Equals(verifiedEvent.EventType, TrialWillEndEventType, StringComparison.Ordinal))
        {
            return false;
        }

        var externalSubscriptionId = verifiedEvent.ExternalSubscriptionId ?? verifiedEvent.ProviderObjectId;
        if (string.IsNullOrWhiteSpace(externalSubscriptionId))
        {
            logger.LogWarning(
                "Stripe trial_will_end event {EventId} carries no subscription identity; skipping reminder",
                verifiedEvent.EventId);
            return true;
        }

        var subscription = await subscriptionQueryService
            .GetByExternalIdAsync(externalSubscriptionId, cancellationToken)
            .ConfigureAwait(false);

        if (subscription is null)
        {
            // A reminder for a subscription this deployment does not track is a no-op, not a
            // failure: the inbox row is marked processed so the retry worker never replays it.
            logger.LogInformation(
                "Stripe trial_will_end event {EventId} references unknown subscription {SubscriptionId}; no reminder sent",
                verifiedEvent.EventId,
                externalSubscriptionId);
            return true;
        }

        var trialEndUtc = ParseTrialEndUtc(verifiedEvent.VerifiedPayload) ?? subscription.TrialEndDate;
        var recipient = await userRepository
            .GetByIdAsync(subscription.CreatedByUserId, cancellationToken)
            .ConfigureAwait(false);

        if (recipient is null || !recipient.IsActive || recipient.IsSuspended || string.IsNullOrWhiteSpace(recipient.Email))
        {
            logger.LogInformation(
                "Trial-ending reminder for subscription {SubscriptionId} skipped: recipient user {UserId} is unavailable or cannot receive mail",
                subscription.Id,
                subscription.CreatedByUserId);
            return true;
        }

        var tenantId = ((ISubscription)subscription).TenantId;
        var metadata = JsonSerializer.Serialize(
            new TrialWillEndEmailMetadata(
                TenantId: tenantId,
                SubscriptionId: subscription.Id,
                UserId: recipient.Id,
                TrialEndUtc: trialEndUtc,
                Amount: subscription.Amount.Amount,
                Currency: subscription.Amount.Currency,
                RecipientEmail: recipient.Email,
                RecipientName: recipient.Name),
            MetadataOptions);

        var daysLeft = trialEndUtc.HasValue
            ? Math.Max(0, (int)Math.Ceiling((trialEndUtc.Value - SystemClock.UtcNow).TotalDays))
            : (int?)null;

        var result = await notificationService
            .SendAsync(
                recipientId: recipient.Id,
                type: NotificationType.Billing,
                title: "Your trial is ending soon",
                message:
                    $"Your subscription trial " +
                    (trialEndUtc.HasValue
                        ? $"ends on {trialEndUtc:yyyy-MM-dd}. "
                        : "is ending soon. ") +
                    $"Your plan ({subscription.Amount.Amount:0.##} {subscription.Amount.Currency}) will start automatically at the end of the trial period.",
                channel: NotificationChannel.Email,
                tenantId: tenantId,
                priority: GameGuild.Notifications.NotificationPriority.Normal,
                referenceEntityId: subscription.Id,
                referenceEntityType: nameof(Subscription),
                metadata: metadata,
                recipientEmail: recipient.Email,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Trial-ending reminder for subscription {SubscriptionId} queued for user {UserId} (event {EventId}, {DaysLeft} day(s) left)",
                subscription.Id,
                recipient.Id,
                verifiedEvent.EventId,
                daysLeft);
        }
        else
        {
            logger.LogInformation(
                "Trial-ending reminder for subscription {SubscriptionId} was not queued: {Reason}",
                subscription.Id,
                result.Error?.Description ?? "preference decision");
        }

        return true;
    }

    /// <summary>
    ///     Reads the provider <c>trial_end</c> unix timestamp from the verified payload. The
    ///     subscription's persisted <c>TrialEndDate</c> is the fallback when the payload omits it.
    /// </summary>
    internal static DateTime? ParseTrialEndUtc(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("data", out var data)
                || !data.TryGetProperty("object", out var subscriptionObject)
                || subscriptionObject.ValueKind != JsonValueKind.Object
                || !subscriptionObject.TryGetProperty("trial_end", out var trialEnd)
                || trialEnd.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(trialEnd.GetInt64()).UtcDateTime;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>Recipient contract for the trial-ending reminder, stored in <see cref="Notification.Metadata"/> and read back by <c>TrialWillEndEmailRenderer</c>.</summary>
public sealed record TrialWillEndEmailMetadata(
    Guid? TenantId,
    Guid SubscriptionId,
    Guid UserId,
    DateTime? TrialEndUtc,
    decimal Amount,
    string Currency,
    string RecipientEmail,
    string? RecipientName);
