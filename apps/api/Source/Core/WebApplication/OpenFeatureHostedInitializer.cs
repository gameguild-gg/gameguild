using OpenFeature;

namespace GameGuild.Core.WebApplication;

/// <summary>
/// Hosted service to initialize OpenFeature providers during application startup.
/// This ensures feature flag providers are properly configured before the application serves requests.
/// </summary>
internal sealed class OpenFeatureHostedInitializer(IServiceProvider serviceProvider) : IHostedService
{
    /// <summary>
    /// Starts the OpenFeature initialization process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var logger = serviceProvider.GetService<ILogger<OpenFeatureHostedInitializer>>();

        try
        {
            var api = Api.Instance;
            var provider = serviceProvider.GetService<FeatureProvider>();

            if (provider != null)
            {
                await api.SetProviderAsync(provider, cancellationToken).ConfigureAwait(false);
                logger?.LogInformation("OpenFeature provider initialized successfully");
            }
            else
            {
                logger?.LogDebug("No FeatureProvider registered, using default NoOpProvider");
            }
        }
        catch (InvalidOperationException ex)
        {
            logger?.LogError(ex, "Failed to initialize OpenFeature provider");
            // Don't rethrow to avoid stopping the application startup
        }
        catch (ArgumentException ex)
        {
            logger?.LogError(ex, "Failed to initialize OpenFeature provider");
            // Don't rethrow to avoid stopping the application startup
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error during OpenFeature provider initialization");
            // Don't rethrow to avoid stopping the application startup
        }
    }

    /// <summary>
    /// Stops the OpenFeature service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
