namespace GameGuild.Common.Services;

/// <summary>
/// Hosted service that manages the Cloudflare Dynamic DNS service lifecycle.
/// </summary>
public class CloudflareExternalIpHostedService : BackgroundService {
    private readonly ILogger<CloudflareExternalIpHostedService> _logger;
    private readonly ICloudflareExternalIpService _cloudflareService;

    public CloudflareExternalIpHostedService(
      ILogger<CloudflareExternalIpHostedService> logger,
      ICloudflareExternalIpService cloudflareService) {
        _logger = logger;
        _cloudflareService = cloudflareService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        try {
            _logger.LogInformation("Starting Cloudflare Dynamic DNS hosted service");
            await _cloudflareService.StartAsync(stoppingToken);

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) {
            _logger.LogInformation("Cloudflare Dynamic DNS hosted service is stopping");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Cloudflare Dynamic DNS hosted service encountered an error");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Stopping Cloudflare Dynamic DNS hosted service");
        await _cloudflareService.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
