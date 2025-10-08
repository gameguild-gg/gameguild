using System;
using System.Collections.Generic;

namespace GameGuild.Modules.Notifications;

/// <summary>
/// Notification template for templated notifications.
/// </summary>
public sealed class NotificationTemplate
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required NotificationType Type { get; init; }
    public required NotificationPriority DefaultPriority { get; init; }
    public required string TitleTemplate { get; init; }
    public required string ContentTemplate { get; init; }
    public string? EmailSubjectTemplate { get; init; }
    public string? EmailBodyTemplate { get; init; }
    public string? PushTitleTemplate { get; init; }
    public string? PushBodyTemplate { get; init; }
    public string? SmsTemplate { get; init; }
    public NotificationChannel DefaultChannels { get; init; } = NotificationChannel.InApp;
    public Dictionary<string, string> Metadata { get; init; } = new();
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
