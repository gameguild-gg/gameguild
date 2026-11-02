using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using NotificationPriority = GameGuild.Notifications.NotificationPriority;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Configuration;

namespace GameGuild.API.Core.Security;

/// <summary>
/// Queues the "your account was temporarily locked" owner notification within the durable
/// consumer's inbox transaction (issue #1083). InApp is always queued; Email only when the
/// user's delivery preferences allow it. The message states the reason (repeated failed
/// sign-in attempts), the unlock timeline (UTC), and a support contact.
/// </summary>
internal sealed class AccountLockoutAlertHandler(
    IUserRepository users,
    INotificationService notifications,
    INotificationPreferenceService preferences,
    IConfiguration configuration) : IIntegrationEventHandler<AccountLockoutDetectedV1>
{
    private const string EnabledPath = "Authentication:SecurityNotifications:Enabled";
    private const string SupportContactPath = "Authentication:SecurityNotifications:SupportContactEmail";

    public async Task HandleAsync(AccountLockoutDetectedV1 @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        if (!configuration.GetValue(EnabledPath, true))
        {
            return;
        }

        var user = await users.GetByIdAsync(@event.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.IsDeleted)
        {
            return;
        }

        // The alert is only useful while the lockout still holds; a delayed delivery that
        // lands after the lockout expired must not confuse the owner.
        var lockoutEndsAtUtc = @event.LockoutEndsAtUtc.ToUniversalTime();
        var remainingMinutes = (int)Math.Ceiling((lockoutEndsAtUtc - SystemClock.UtcNow).TotalMinutes);
        if (remainingMinutes <= 0)
        {
            return;
        }

        var message = ComposeMessage(lockoutEndsAtUtc, remainingMinutes, @event.FailedAttemptThreshold);

        await QueueAsync(NotificationChannel.InApp, null).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            // Email follows the user's delivery preferences; lockout alerts are Urgent and
            // transactional, bypassing mutes/quiet hours unless email is disabled upstream.
            var decision = await preferences.DecideDeliveryAsync(
                user.Id, NotificationType.Security, NotificationChannel.Email, NotificationPriority.Urgent, cancellationToken).ConfigureAwait(false);
            if (decision.Action == NotificationDeliveryAction.Send)
            {
                await QueueAsync(NotificationChannel.Email, user.Email).ConfigureAwait(false);
            }
        }

        async Task QueueAsync(NotificationChannel channel, string? recipientEmail)
        {
            var result = await notifications.SendAsync(user.Id, NotificationType.Security,
                "Your account was temporarily locked",
                message,
                channel, tenantId: null, priority: NotificationPriority.Urgent,
                referenceEntityId: @event.EventId, referenceEntityType: nameof(AccountLockoutDetectedV1),
                recipientEmail: recipientEmail, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result is null || result.IsFailure)
            {
                throw new InvalidOperationException("The account-lockout security alert was not durably queued.");
            }
        }
    }

    /// <summary>
    /// Redacted copy: reason, unlock timeline, and support contact. The message never
    /// includes the identifier, the attempt IP addresses, or the user agents.
    /// </summary>
    private string ComposeMessage(DateTime lockoutEndsAtUtc, int remainingMinutes, int failedAttemptThreshold) =>
        configuration[SupportContactPath] is { Length: > 0 } supportContact
            ? $"We temporarily locked your account after {failedAttemptThreshold} or more failed sign-in attempts. " +
              $"Sign-in unlocks at {lockoutEndsAtUtc:u} (UTC) — about {remainingMinutes} minute{(remainingMinutes == 1 ? string.Empty : "s")} from now. " +
              $"If this was not you, contact support at {supportContact} and review your active sessions."
            : $"We temporarily locked your account after {failedAttemptThreshold} or more failed sign-in attempts. " +
              $"Sign-in unlocks at {lockoutEndsAtUtc:u} (UTC) — about {remainingMinutes} minute{(remainingMinutes == 1 ? string.Empty : "s")} from now. " +
              "If this was not you, contact support and review your active sessions.";
}
