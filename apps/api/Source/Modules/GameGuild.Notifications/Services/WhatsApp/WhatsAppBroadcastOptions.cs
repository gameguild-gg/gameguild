namespace GameGuild.Notifications.Services.WhatsApp;

/// <summary>
///     Broadcast engine tuning (issue #55). Bound to configuration section
///     "Notifications:WhatsApp:Broadcast". The recipient cap bounds the blast radius of
///     one campaign and the send batch size bounds how many provider calls one sweep
///     makes before persisting progress.
/// </summary>
public sealed class WhatsAppBroadcastOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:WhatsApp:Broadcast";

    /// <summary>Maximum recipients a single broadcast may resolve; larger audiences are rejected.</summary>
    public int MaxRecipientsPerBroadcast { get; set; } = 500;

    /// <summary>Recipients attempted per persisted batch during a send sweep.</summary>
    public int SendBatchSize { get; set; } = 100;

    /// <summary>Scheduler sweep interval for due scheduled broadcasts.</summary>
    public TimeSpan SchedulerInterval { get; set; } = TimeSpan.FromMinutes(1);
}
