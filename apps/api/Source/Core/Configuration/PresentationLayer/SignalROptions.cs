namespace GameGuild;

public class SignalROptions {
  public bool EnableDetailedErrors { get; set; }

  public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);

  public TimeSpan ClientTimeoutInterval { get; set; } = TimeSpan.FromSeconds(30);

  public long MaximumReceiveMessageSize { get; set; } = 32 * 1024; // 32KB

  public void Validate() {
    if (KeepAliveInterval <= TimeSpan.Zero) throw new InvalidOperationException("Keep alive interval must be greater than zero.");

    if (ClientTimeoutInterval <= TimeSpan.Zero) throw new InvalidOperationException("Client timeout interval must be greater than zero.");

    if (ClientTimeoutInterval <= KeepAliveInterval) throw new InvalidOperationException("Client timeout interval must be greater than keep alive interval.");

    if (MaximumReceiveMessageSize <= 0) throw new InvalidOperationException("Maximum receive message size must be greater than zero.");
  }
}
