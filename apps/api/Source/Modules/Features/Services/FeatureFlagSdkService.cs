using System.Text.Json;
using GameGuild.Modules.Features.Entities;

namespace GameGuild.Modules.Features.Services;

/// <summary>
/// SDK configuration for client-side and edge evaluation
/// </summary>
public class SdkConfiguration
{
    public string Version { get; set; } = "1.0.0";
    public DateTimeOffset GeneratedAt { get; set; }
    public string Environment { get; set; } = string.Empty;
    public List<SdkFeatureFlag> Features { get; set; } = new();
    public SdkSettings Settings { get; set; } = new();
}

public class SdkFeatureFlag
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public string? EnabledValue { get; set; }
    public int RolloutPercentage { get; set; }
    public List<SdkTargetingRule> TargetingRules { get; set; } = new();
    public bool IsKillSwitch { get; set; }
}

public class SdkTargetingRule
{
    public string Attribute { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public List<string> Values { get; set; } = new();
}

public class SdkSettings
{
    public int RefreshIntervalSeconds { get; set; } = 300;
    public bool EnableAnalytics { get; set; } = true;
    public bool EnableCaching { get; set; } = true;
    public int CacheTtlSeconds { get; set; } = 600;
}

/// <summary>
/// Service for generating client-side SDK configurations
/// </summary>
public interface IFeatureFlagSdkService
{
    /// <summary>
    /// Generates SDK configuration for client-side evaluation
    /// </summary>
    Task<SdkConfiguration> GenerateSdkConfigurationAsync(string environment, string? tenantId = null);

    /// <summary>
    /// Generates edge-optimized configuration (minimal payload)
    /// </summary>
    Task<string> GenerateEdgeConfigurationAsync(string environment, string? tenantId = null);

    /// <summary>
    /// Validates SDK configuration integrity
    /// </summary>
    Task<bool> ValidateSdkConfigurationAsync(string configJson);
}

/// <summary>
/// Implementation of SDK configuration generation
/// </summary>
public class FeatureFlagSdkService : IFeatureFlagSdkService
{
    private readonly IFeatureFlagRepository _repository;
    private readonly IFeatureFlagEncryptionService _encryptionService;

    public FeatureFlagSdkService(
        IFeatureFlagRepository repository,
        IFeatureFlagEncryptionService encryptionService)
    {
        _repository = repository;
        _encryptionService = encryptionService;
    }

    public async Task<SdkConfiguration> GenerateSdkConfigurationAsync(string environment, string? tenantId = null)
    {
        var flags = await _repository.GetByEnvironmentAsync(environment);

        if (!string.IsNullOrEmpty(tenantId))
        {
            flags = flags.Where(f => f.TenantId == Guid.Parse(tenantId) || f.IsGlobal).ToList();
        }

        var sdkFeatures = new List<SdkFeatureFlag>();

        foreach (var flag in flags)
        {
            // Decrypt sensitive values before sending to SDK
            if (flag.RequiresEncryption)
            {
                await flag.DecryptSensitiveDataAsync(_encryptionService);
            }

            var sdkFlag = new SdkFeatureFlag
            {
                Key = flag.Key,
                Name = flag.Name,
                IsEnabled = flag.IsEnabled,
                Type = flag.Type.ToString(),
                DefaultValue = flag.DefaultValue,
                EnabledValue = flag.EnabledValue,
                RolloutPercentage = flag.RolloutPercentage,
                IsKillSwitch = flag.IsKillSwitch,
                TargetingRules = flag.Targets?.Select(t => new SdkTargetingRule
                {
                    Attribute = t.Attribute ?? string.Empty,
                    Operator = t.Operator ?? "equals",
                    Values = string.IsNullOrEmpty(t.Value) ? new List<string>() : new List<string> { t.Value }
                }).ToList() ?? new List<SdkTargetingRule>()
            };

            sdkFeatures.Add(sdkFlag);
        }

        return new SdkConfiguration
        {
            Version = "1.0.0",
            GeneratedAt = DateTimeOffset.UtcNow,
            Environment = environment,
            Features = sdkFeatures,
            Settings = new SdkSettings
            {
                RefreshIntervalSeconds = 300,
                EnableAnalytics = true,
                EnableCaching = true,
                CacheTtlSeconds = 600
            }
        };
    }

    public async Task<string> GenerateEdgeConfigurationAsync(string environment, string? tenantId = null)
    {
        var config = await GenerateSdkConfigurationAsync(environment, tenantId);

        // Minimize payload for edge evaluation
        var minimalConfig = new
        {
            v = config.Version,
            e = config.Environment,
            t = config.GeneratedAt.ToUnixTimeSeconds(),
            f = config.Features.Select(f => new
            {
                k = f.Key,
                e = f.IsEnabled,
                r = f.RolloutPercentage,
                t = f.Type,
                dv = f.DefaultValue,
                ev = f.EnabledValue,
                ks = f.IsKillSwitch
            })
        };

        return JsonSerializer.Serialize(minimalConfig, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    public async Task<bool> ValidateSdkConfigurationAsync(string configJson)
    {
        try
        {
            var config = JsonSerializer.Deserialize<SdkConfiguration>(configJson);

            if (config == null)
                return false;

            // Validate version format
            if (!Version.TryParse(config.Version, out _))
                return false;

            // Validate features
            foreach (var feature in config.Features)
            {
                if (string.IsNullOrEmpty(feature.Key))
                    return false;

                if (feature.RolloutPercentage < 0 || feature.RolloutPercentage > 100)
                    return false;
            }

            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Extension method for repository to get flags by environment
/// </summary>
public static class FeatureFlagRepositoryExtensions
{
    public static async Task<List<FeatureFlag>> GetByEnvironmentAsync(this IFeatureFlagRepository repository, string environment)
    {
        var allFlags = await repository.GetAllAsync();
        return allFlags.Where(f => f.Environment == environment).ToList();
    }
}
