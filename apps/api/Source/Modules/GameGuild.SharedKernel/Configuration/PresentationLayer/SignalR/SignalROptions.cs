namespace GameGuild.Configuration.PresentationLayer.SignalR;

/// <summary>
///     Configuration options for the SignalR real-time communication hub.
/// </summary>
public sealed class SignalROptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "SignalR";

    /// <summary>
    ///     The URL path for the SignalR hub endpoint.
    /// </summary>
    public string HubPath { get; set; } = "/hub";

    /// <summary>
    ///     Whether to include detailed error messages in hub responses. Should be <c>false</c> in production.
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    ///     Interval at which the server sends keep-alive pings to connected clients.
    /// </summary>
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    ///     Maximum time a client can remain inactive before the server closes the connection.
    /// </summary>
    public TimeSpan ClientTimeoutInterval { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>
    ///     Maximum size (in bytes) of a single incoming hub message. <c>null</c> means unlimited.
    /// </summary>
    public long? MaximumReceiveMessageSize { get; set; } = null;

    /// <summary>
    ///     Creates a <see cref="SignalROptions" /> instance with default values.
    /// </summary>
    public static SignalROptions CreateDefault() { return new SignalROptions(); }
}
