using GameGuild.CQRS.Publishers;

namespace GameGuild.CQRS;

/// <summary>
///     CQRS configuration
/// </summary>
public class CqrsConfiguration
{
    /// <summary>
    ///     Gets or sets the notification publisher
    /// </summary>
    public INotificationPublisher NotificationPublisher { get; set; } = new ForeachAwaitPublisher();
}
