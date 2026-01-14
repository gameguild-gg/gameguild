using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Background service for automatic JWT key rotation on a scheduled basis
/// </summary>
public class KeyRotationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KeyRotationBackgroundService> _logger;
    private readonly KeyRotationOptions _options;

    public KeyRotationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<KeyRotationBackgroundService> logger,
        IOptions<KeyRotationOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JWT Key Rotation Background Service started");

        // Initialize keys on startup
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var keyRotationService = scope.ServiceProvider.GetRequiredService<IKeyRotationService>();
            await keyRotationService.InitializeAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize JWT signing keys");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.CheckInterval, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var keyRotationService = scope.ServiceProvider.GetRequiredService<IKeyRotationService>();

                // Check if rotation is needed
                var activeKey = await keyRotationService.GetActiveSigningKeyAsync(stoppingToken);
                if (activeKey == null)
                {
                    _logger.LogWarning("No active signing key found. Rotating...");
                    await keyRotationService.RotateKeyAsync("missing-active-key", _options.KeyValidityDays, stoppingToken);
                }
                else
                {
                    var timeUntilExpiry = activeKey.ExpiresAt - DateTime.UtcNow;
                    if (timeUntilExpiry <= _options.RotationThreshold)
                    {
                        _logger.LogInformation("Active key expires in {TimeUntilExpiry}. Rotating...", timeUntilExpiry);
                        await keyRotationService.RotateKeyAsync("scheduled-rotation", _options.KeyValidityDays, stoppingToken);
                    }
                }

                // Cleanup expired keys
                await keyRotationService.CleanupExpiredKeysAsync(_options.ExpiredKeyRetentionDays, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during JWT key rotation check");
                // Continue running despite errors
            }
        }

        _logger.LogInformation("JWT Key Rotation Background Service stopped");
    }
}

/// <summary>
///     Configuration options for JWT key rotation
/// </summary>
public class KeyRotationOptions
{
    /// <summary>
    ///     How often to check if rotation is needed (default: every hour)
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    ///     How long keys are valid after creation (default: 90 days)
    /// </summary>
    public int KeyValidityDays { get; set; } = 90;

    /// <summary>
    ///     Rotate when this much time remains before expiry (default: 7 days)
    /// </summary>
    public TimeSpan RotationThreshold { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    ///     Keep expired keys for this many days for audit purposes (default: 30 days)
    /// </summary>
    public int ExpiredKeyRetentionDays { get; set; } = 30;
}
