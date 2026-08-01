using OpenFeature;

namespace GameGuild.API;

internal sealed class OpenFeatureHostedInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var logger = serviceProvider.GetService<ILogger<OpenFeatureHostedInitializer>>();

        try
        {
            var api = Api.Instance;
            var provider = serviceProvider.GetService<FeatureProvider>();

            if (provider != null)
            {
                await api.SetProviderAsync(provider).ConfigureAwait(false);
                logger?.LogInformation("OpenFeature provider initialized successfully");
            }
            else { logger?.LogDebug("No FeatureProvider registered, using default NoOpProvider"); }
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
    }

    public Task StopAsync(CancellationToken cancellationToken) { return Task.CompletedTask; }
}
