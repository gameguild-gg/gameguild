namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Admin-facing read/maintenance surface for email deliverability: event feed,
/// suppressions, dead letters, requeue and per-notification timeline.
/// </summary>
public interface IEmailDeliveryAdminService
{
    /// <summary>
    /// Gets the delivery event feed, newest first. The email filter is normalized
    /// via <see cref="EmailAddressNormalizer"/> before comparing (events store normalized
    /// RecipientEmail values).
    /// </summary>
    Task<Result<PagedResult<EmailDeliveryEvent>>> GetEventsAsync(
        int skip,
        int take,
        string? eventType = null,
        string? email = null,
        string? providerMessageId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suppressions, newest first. Active-only unless <paramref name="includeReleased"/>.
    /// </summary>
    Task<Result<PagedResult<EmailSuppression>>> GetSuppressionsAsync(
        int skip,
        int take,
        bool includeReleased = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the active suppression for an address (admin unsuppress). The input email is
    /// normalized before lookup. Returns false when no active suppression exists — idempotent.
    /// </summary>
    Task<Result<bool>> ReleaseSuppressionAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead-lettered notifications, newest first. The email filter compares normalized
    /// on both sides (Notification.RecipientEmail stores raw values).
    /// </summary>
    Task<Result<PagedResult<Notification>>> GetDeadLettersAsync(
        int skip,
        int take,
        string? type = null,
        string? email = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requeues a dead-lettered notification for another delivery attempt. Fails with
    /// Conflict when the row is not dead-lettered or an active suppression matches the
    /// resolved recipient address.
    /// </summary>
    Task<Result<Notification>> RequeueAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the delivery timeline of a notification (its events ordered by OccurredAt).
    /// ProviderMessageId is null for digest bundles / never-sent rows, with an empty event list.
    /// </summary>
    Task<Result<EmailDeliveryTimeline>> GetTimelineAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Timeline payload for one notification: its provider correlation id (null when the row
/// never reached the provider) and the correlated events, oldest first.
/// </summary>
public sealed record EmailDeliveryTimeline(
    string? ProviderMessageId,
    IReadOnlyList<EmailDeliveryEvent> Events);
