namespace GameGuild;

/// <summary>
///     Cross-module contract for publishing user-facing in-app notifications without creating module cycles.
/// </summary>
public interface IApplicationNotificationPublisher
{
    Task<ApplicationNotificationPublishResult> PublishAsync(
        ApplicationNotificationMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record ApplicationNotificationMessage(
    Guid RecipientId,
    string Title,
    string Message,
    string Type,
    string Priority,
    Guid? TenantId = null,
    string? ActionUrl = null,
    Guid? ReferenceEntityId = null,
    string? ReferenceEntityType = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record ApplicationNotificationPublishResult(bool IsSuccess, Guid? NotificationId, string? ErrorMessage)
{
    public static ApplicationNotificationPublishResult Success(Guid notificationId) => new(true, notificationId, null);

    public static ApplicationNotificationPublishResult Failure(string errorMessage) => new(false, null, errorMessage);
}
