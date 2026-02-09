namespace GameGuild.Notifications.Services;

/// <summary>
/// Service for managing notification templates
/// </summary>
public interface INotificationTemplateService
{
    /// <summary>
    /// Gets a notification template by code
    /// </summary>
    Task<Result<NotificationTemplate>> GetTemplateByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all notification templates with optional filtering
    /// </summary>
    Task<Result<IEnumerable<NotificationTemplate>>> GetTemplatesAsync(
        string? category = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a notification template
    /// </summary>
    Task<Result<NotificationTemplate>> CreateTemplateAsync(
        string code,
        string name,
        NotificationType type,
        NotificationChannel channel,
        string titleTemplate,
        string messageTemplate,
        string? description = null,
        string? actionUrlTemplate = null,
        string? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a notification template
    /// </summary>
    Task<Result<NotificationTemplate>> UpdateTemplateAsync(
        Guid templateId,
        string? titleTemplate = null,
        string? messageTemplate = null,
        string? actionUrlTemplate = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces placeholders in a template string
    /// </summary>
    string ReplacePlaceholders(string template, Dictionary<string, string> placeholders);
}
