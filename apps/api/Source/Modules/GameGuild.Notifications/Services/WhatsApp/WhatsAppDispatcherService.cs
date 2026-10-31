using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Notifications.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Notifications.Services.WhatsApp;

/// <summary>
///     Core WhatsApp dispatch pipeline (issue #54), unit-testable without a
///     BackgroundService host — the out-of-band twin of
///     <see cref="Sms.SmsDispatcherService"/>. Sweeps Pending
///     <see cref="NotificationChannel.WhatsApp"/> rows, claims them (Sending), resolves
///     the destination phone from protected metadata or the user profile, sends through
///     <see cref="IWhatsAppProvider"/> (template send when the row carries an allowlisted
///     template name, free-form text otherwise), and applies retry/backoff and
///     deadlettering. While the channel is unconfigured the NoOp provider declines every
///     send, so rows retry observably and deadletter instead of silently vanishing.
/// </summary>
public sealed class WhatsAppDispatcherService(
    IApplicationDbContext context,
    IWhatsAppProvider provider,
    IWhatsAppRecipientPhoneResolver recipientPhoneResolver,
    NotificationMetadataProtector metadataProtector,
    IOptions<WhatsAppDispatcherOptions> options,
    IOptions<WhatsAppSettings> settings,
    ILogger<WhatsAppDispatcherService> logger)
{
    private static readonly TimeSpan SendingReclaimWindow = TimeSpan.FromMinutes(10);
    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Sweeps one batch of due WhatsApp rows and returns how many were processed.</summary>
    public async Task<int> SweepOnceAsync(CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var now = SystemClock.UtcNow;
        var reclaimCutoff = now.Subtract(SendingReclaimWindow);

        var batch = await context.Set<Notification>()
            .Where(n => n.Channel == NotificationChannel.WhatsApp
                && (n.DeliveryStatus == NotificationDeliveryStatus.Pending
                        && (n.NextAttemptAt == null || n.NextAttemptAt <= now)
                        && (n.ScheduledAt == null || n.ScheduledAt <= now)
                    || (n.DeliveryStatus == NotificationDeliveryStatus.Sending && n.UpdatedAt <= reclaimCutoff)))
            .OrderBy(n => n.CreatedAt)
            .Take(opts.SweepBatchSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var processed = 0;
        foreach (var notification in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await DispatchAsync(notification, opts, cancellationToken).ConfigureAwait(false);
                processed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "WhatsApp delivery failed. NotificationId: {NotificationId}, Type: {Type}",
                    notification.Id, notification.Type);
                try
                {
                    HandleFailure(notification, ex, opts);
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception saveEx)
                {
                    // Row stays Sending and is reclaimed by a later sweep.
                    logger.LogError(saveEx, "Failed to persist WhatsApp delivery failure. NotificationId: {NotificationId}",
                        notification.Id);
                }
            }
        }

        return processed;
    }

    private async Task DispatchAsync(Notification notification, WhatsAppDispatcherOptions opts, CancellationToken cancellationToken)
    {
        // Keep the metadata envelope protected before any lifecycle write.
        metadataProtector.ProtectForStorage(notification);

        // 1. Claim (persisted before the external call so a crash mid-send is reclaimed, not lost).
        if (notification.DeliveryStatus == NotificationDeliveryStatus.Pending)
        {
            notification.ClaimForSending();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // 2. Recipient: metadata phone first, user profile fallback. An unresolvable
        //    phone is a permanent error — deadletter, do not retry.
        var toPhone = await recipientPhoneResolver.ResolveAsync(notification, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(toPhone))
        {
            notification.MarkDeadLettered("Recipient phone could not be resolved from notification metadata or user profile");
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogError("Deadlettered WhatsApp notification without recipient. NotificationId: {NotificationId}", notification.Id);
            return;
        }

        // 3. Send: template send when the row carries an allowlisted template name
        //    (required for business-initiated conversations), free-form text otherwise.
        var templateName = ReadTemplateName(notification);
        Result<WhatsAppSendResult> sendResult = templateName is not null && settings.Value.IsTemplateAllowed(templateName)
            ? await provider.SendTemplateAsync(
                new WhatsAppTemplateMessage(toPhone, templateName, null, null),
                cancellationToken).ConfigureAwait(false)
            : await provider.SendMessageAsync(
                new WhatsAppOutboundMessage(toPhone, notification.Message),
                cancellationToken).ConfigureAwait(false);

        if (sendResult.IsFailure)
        {
            throw new InvalidOperationException($"The WhatsApp provider declined the message for delivery: {sendResult.Error.Description}");
        }

        notification.MarkDeliverySent(sendResult.Value.ProviderMessageId);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("WhatsApp delivered. NotificationId: {NotificationId}, Type: {Type}",
            notification.Id, notification.Type);
    }

    /// <summary>
    ///     Extracts the optional template name from notification metadata
    ///     (<c>{"phoneNumber":"...","templateName":"..."}</c>), decrypting the protected
    ///     envelope for transactional types. Free-form text is sent when absent.
    /// </summary>
    private string? ReadTemplateName(Notification notification)
    {
        string? metadata;
        try
        {
            metadata = NotificationMetadataProtector.RequiresProtection(notification.Type)
                ? metadataProtector.GetForRendering(notification, notification.Type)
                : notification.Metadata;
        }
        catch (Exception exception) when (exception is System.Security.Cryptography.CryptographicException or FormatException)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<MetadataContract>(metadata, MetadataJsonOptions);
            return string.IsNullOrWhiteSpace(payload?.TemplateName) ? null : payload.TemplateName.Trim();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void HandleFailure(Notification notification, Exception ex, WhatsAppDispatcherOptions opts)
    {
        if (notification.AttemptCount + 1 >= opts.MaxAttempts)
        {
            notification.MarkDeadLettered($"delivery failed after {notification.AttemptCount + 1} attempts: {Truncate(ex.Message)}");
            return;
        }

        var backoff = opts.BackoffSchedule.Length == 0
            ? TimeSpan.FromMinutes(1)
            : opts.BackoffSchedule[Math.Min(notification.AttemptCount, opts.BackoffSchedule.Length - 1)];
        notification.MarkDeliveryAttemptFailed(Truncate(ex.Message), SystemClock.UtcNow.Add(backoff));
    }

    private static string Truncate(string value) =>
        value.Length <= 200 ? value : value[..200];

    private sealed record MetadataContract(string? PhoneNumber, string? TemplateName);
}
