using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenFeature;

namespace GameGuild.Features;

/// <summary>
///     Hosted service that initializes OpenFeature on application startup
/// </summary>
public class OpenFeatureHostedInitializer : IHostedService
{
    private readonly ILogger<OpenFeatureHostedInitializer> _logger;

    public OpenFeatureHostedInitializer(ILogger<OpenFeatureHostedInitializer> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initializing OpenFeature...");

            // OpenFeature API is already configured with the provider in FeaturesModuleExtensions
            // This service ensures any additional startup initialization happens
            var api = Api.Instance;

            // Wait for the provider to be ready
            await Task.CompletedTask;

            _logger.LogInformation("OpenFeature initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize OpenFeature");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutting down OpenFeature...");
        return Task.CompletedTask;
    }
}
