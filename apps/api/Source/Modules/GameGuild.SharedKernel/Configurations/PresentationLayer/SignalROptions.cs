namespace GameGuild.SharedKernel.Configuration;

public class SignalROptions : BaseOptions
{
    public string HubPath { get; set; } = "/hub";

    public bool EnableDetailedErrors { get; set; } = false;

    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);

    public TimeSpan ClientTimeoutInterval { get; set; } = TimeSpan.FromSeconds(120);

    public long? MaximumReceiveMessageSize { get; set; } = null;

    public static SignalROptions CreateDefault() { return new SignalROptions(); }
}
