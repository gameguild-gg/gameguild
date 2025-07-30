namespace GameGuild.Common.Services;

/// <summary>
/// Service for updating Cloudflare DNS records with external IP address.
/// </summary>
public interface ICloudflareExternalIpService {
    /// <summary>
    /// Starts the periodic IP check and DNS update service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the periodic service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually checks and updates the external IP address once.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateExternalIpAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current external IP address.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<string?> GetExternalIpAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current service status.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the last known external IP address.
    /// </summary>
    string? LastKnownIp { get; }

    /// <summary>
    /// Gets the last update timestamp.
    /// </summary>
    DateTime? LastUpdate { get; }
}
