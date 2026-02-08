using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameGuild.Assets.BackgroundServices;

/// <summary>
/// Background service for cleaning up stale transformed asset cache.
/// </summary>
public class TransformedAssetCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransformedAssetCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6);
    private readonly TimeSpan _maxAge = TimeSpan.FromDays(7);

    public TransformedAssetCleanupService(
        IServiceProvider serviceProvider,
        ILogger<TransformedAssetCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Transformed Asset Cleanup service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during transformed asset cleanup");
            }

            await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var transformedRepository = scope.ServiceProvider.GetRequiredService<ITransformedAssetRepository>();
        var storageService = scope.ServiceProvider.GetRequiredService<IAssetStorageService>();
        var storageOptions = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<AssetStorageOptions>>().Value;

        // Get stale transformed assets
        var staleAssets = await transformedRepository.GetStaleAssetsAsync(_maxAge, 100, ct).ConfigureAwait(false);

        if (staleAssets.Count == 0)
        {
            _logger.LogDebug("No stale transformed assets to clean up");
            return;
        }

        _logger.LogInformation(
            "Cleaning up {Count} stale transformed assets",
            staleAssets.Count);

        var deleted = 0;
        var failed = 0;

        foreach (var asset in staleAssets)
        {
            try
            {
                // Delete from storage
                await storageService.DeleteAsync(
                    storageOptions.TransformedBucketName,
                    asset.ObjectKey,
                    ct).ConfigureAwait(false);

                // Delete record
                await transformedRepository.DeleteAsync(asset.Id, ct).ConfigureAwait(false);

                deleted++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(
                    ex,
                    "Failed to delete transformed asset {AssetId}",
                    asset.Id);
                throw;
            }
        }

        _logger.LogInformation(
            "Transformed asset cleanup completed: {Deleted} deleted, {Failed} failed",
            deleted, failed);
    }
}
