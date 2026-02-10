using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Features;

/// <summary>
///     Service for retrieving feature flag configurations for SDKs and client applications.
///     Implements IFeatureFlagConfigurationService following the Interface Segregation Principle.
/// </summary>
public class FeatureFlagConfigurationService(IFeatureFlagQueryRepository queryRepository, ILogger<FeatureFlagConfigurationService> logger, IOptions<FeatureFlagOptions> options, IMemoryCache? cache = null)
    : IFeatureFlagConfigurationService
{
    private readonly ILogger<FeatureFlagConfigurationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly FeatureFlagOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));

    private readonly IFeatureFlagQueryRepository _queryRepository = queryRepository ?? throw new ArgumentNullException(nameof(queryRepository));

    /// <inheritdoc />
    public async Task<FeatureFlagConfig?> GetConfigAsync(string featureKey, string? environment = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureKey);

        environment ??= _options.DefaultEnvironment;

        try
        {
            var featureFlag = await GetCachedFeatureFlagAsync(featureKey, cancellationToken).ConfigureAwait(false);

            if (featureFlag == null)
            {
                _logger.LogWarning("Feature flag '{FeatureKey}' not found", featureKey);

                return null;
            }

            // Filter by environment if specified
            if (!string.IsNullOrEmpty(featureFlag.Environment) && !string.Equals(featureFlag.Environment, environment, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Feature flag '{FeatureKey}' not available for environment '{Environment}'", featureKey, environment);

                return null;
            }

            return MapToConfig(featureFlag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving config for feature '{FeatureKey}'", featureKey);

            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FeatureFlagConfig>> GetAllConfigsAsync(string? environment = null, CancellationToken cancellationToken = default)
    {
        environment ??= _options.DefaultEnvironment;

        try
        {
            var featureFlags = await GetCachedFeatureFlagsByEnvironmentAsync(environment, cancellationToken).ConfigureAwait(false);

            return featureFlags.Select(MapToConfig).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all configs for environment '{Environment}'", environment);

            return [];
        }
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, FeatureFlagConfig>> GetConfigsAsync(IEnumerable<string> featureKeys, string? environment = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(featureKeys);

        var keysList = featureKeys.ToList();

        if (!keysList.Any()) { return new Dictionary<string, FeatureFlagConfig>(); }

        environment ??= _options.DefaultEnvironment;

        try
        {
            // Batch query for better performance
            var featureFlags = await _queryRepository.GetByKeysAsync(keysList, cancellationToken).ConfigureAwait(false);

            var result = new Dictionary<string, FeatureFlagConfig>();

            foreach (var featureFlag in featureFlags)
            {
                // Filter by environment
                if (!string.IsNullOrEmpty(featureFlag.Environment) && !string.Equals(featureFlag.Environment, environment, StringComparison.OrdinalIgnoreCase)) { continue; }

                result[featureFlag.Key] = MapToConfig(featureFlag);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configs for multiple features");

            return new Dictionary<string, FeatureFlagConfig>();
        }
    }

    /// <inheritdoc />
    public async Task<string> GetConfigHashAsync(string? environment = null, CancellationToken cancellationToken = default)
    {
        environment ??= _options.DefaultEnvironment;

        try
        {
            var configs = await GetAllConfigsAsync(environment, cancellationToken).ConfigureAwait(false);

            // Serialize configs to JSON for consistent hashing
            var configsJson = JsonSerializer.Serialize(
                configs.OrderBy(c => c.Key), // Sort for deterministic hashing
                new JsonSerializerOptions { WriteIndented = false }
            );

            // Generate SHA256 hash
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(configsJson));

            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating config hash for environment '{Environment}'", environment);

            // Return a timestamp-based hash as fallback
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(SystemClock.UtcNow.ToString("O")));
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasConfigChangedAsync(string currentHash, string? environment = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentHash);

        try
        {
            var latestHash = await GetConfigHashAsync(environment, cancellationToken).ConfigureAwait(false);

            return !string.Equals(currentHash, latestHash, StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if config has changed");

            // Assume changed on error to force refresh
            return true;
        }
    }

    #region Private Helper Methods

    private async Task<FeatureFlag?> GetCachedFeatureFlagAsync(string key, CancellationToken cancellationToken)
    {
        if (!_options.EnableCaching || cache == null) { return await _queryRepository.GetByKeyAsync(key, cancellationToken).ConfigureAwait(false); }

        var cacheKey = $"{FeatureFlagConstants.CacheKeys.ConfigPrefix}{key}";

        return await cache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheTtlMinutes);

                return await _queryRepository.GetByKeyAsync(key, cancellationToken).ConfigureAwait(false);
            }
        );
    }

    private async Task<IEnumerable<FeatureFlag>> GetCachedFeatureFlagsByEnvironmentAsync(string environment, CancellationToken cancellationToken)
    {
        if (!_options.EnableCaching || cache == null) { return await _queryRepository.GetByEnvironmentAsync(environment, cancellationToken).ConfigureAwait(false); }

        var cacheKey = $"{FeatureFlagConstants.CacheKeys.EnvironmentPrefix}{environment}";

        return await cache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheTtlMinutes);

                return await _queryRepository.GetByEnvironmentAsync(environment, cancellationToken).ConfigureAwait(false);
            }
        ) ?? [];
    }

    private static FeatureFlagConfig MapToConfig(FeatureFlag featureFlag)
    {
        return new FeatureFlagConfig
        {
            Key = featureFlag.Key,
            Name = featureFlag.Name,
            Description = featureFlag.Description,
            IsEnabled = featureFlag.IsEnabled,
            Type = EntityModelMapper.ToModel(featureFlag.Type),
            DefaultValue = featureFlag.DefaultValue,
            EnabledValue = featureFlag.EnabledValue,
            IsGlobal = featureFlag.IsGlobal,
            RolloutPercentage = featureFlag.RolloutPercentage,
            Environment = featureFlag.Environment,
            LastModified = featureFlag.UpdatedAt
        };
    }

    #endregion
}
