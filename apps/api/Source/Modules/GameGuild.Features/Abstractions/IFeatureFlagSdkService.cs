namespace GameGuild.Features;

/// <summary>
///     Feature flag SDK configuration interface
/// </summary>
public interface IFeatureFlagSdkService
{
    Task<SdkConfiguration> GenerateSdkConfigurationAsync(string environment, string? tenantId = null, CancellationToken cancellationToken = default);

    Task<SdkEndpoints> GetSdkEndpointsAsync(CancellationToken cancellationToken = default);

    Task<string> GenerateApiKeyAsync(string environment, string? tenantId = null, CancellationToken cancellationToken = default);

    Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}
