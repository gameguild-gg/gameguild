namespace GameGuild.Features;

/// <summary>
///     Constants used throughout the feature flag system
/// </summary>
public static class FeatureFlagConstants
{
    /// <summary>
    ///     Default environment name
    /// </summary>
    public const string DefaultEnvironment = "production";

    /// <summary>
    ///     Maximum rollout percentage value
    /// </summary>
    public const int MaxRolloutPercentage = 100;

    /// <summary>
    ///     Minimum rollout percentage value
    /// </summary>
    public const int MinRolloutPercentage = 0;

    /// <summary>
    ///     Default rollout percentage (fully rolled out)
    /// </summary>
    public const int DefaultRolloutPercentage = 100;

    /// <summary>
    ///     Default salt value for rollout hash calculation
    /// </summary>
    public const string DefaultRolloutSalt = "default";

    /// <summary>
    ///     Anonymous identifier used when user/tenant context is not available
    /// </summary>
    public const string AnonymousIdentifier = "anonymous";

    /// <summary>
    ///     Target type identifiers
    /// </summary>
    public static class TargetTypes
    {
        public const string Tenant = "tenant";

        public const string User = "user";

        public const string Plan = "plan";

        public const string Country = "country";

        public const string Environment = "environment";

        public const string Role = "role";

        public const string Custom = "custom";
    }

    /// <summary>
    ///     Cache key prefixes
    /// </summary>
    public static class CacheKeys
    {
        public const string FeatureFlagPrefix = "feature:";

        public const string ConfigPrefix = "config:";

        public const string AnalyticsPrefix = "analytics:";

        public const string SdkPrefix = "sdk:";

        public const string EnvironmentPrefix = "env:";
    }

    /// <summary>
    ///     Feature flag type identifiers
    /// </summary>
    public static class FlagTypes
    {
        public const string Toggle = "toggle";

        public const string Experiment = "experiment";

        public const string Rollout = "rollout";

        public const string Permission = "permission";

        public const string KillSwitch = "killswitch";
    }

    /// <summary>
    ///     Asset module feature flag keys (Architecture Doc D.3)
    /// </summary>
    public static class AssetFeatureFlags
    {
        /// <summary>Enable/disable image transformations globally</summary>
        public const string TransformationsEnabled = "asset:transformations:enabled";

        /// <summary>Comma-separated list of allowed transformation types</summary>
        public const string AllowedTransformations = "asset:transformations:allowed";

        /// <summary>Maximum dimension (width/height) for image transformations</summary>
        public const string MaxTransformDimension = "asset:transform:max:dimension";

        /// <summary>Hours before signed download URLs expire</summary>
        public const string DownloadWindowHours = "asset:download:window:hours";

        /// <summary>Maximum hotlink requests allowed per hour</summary>
        public const string HotlinkLimitPerHour = "asset:hotlink:limit:per:hour";

        /// <summary>Enable/disable perceptual deduplication for images</summary>
        public const string PerceptualDedupEnabled = "asset:dedup:perceptual:enabled";

        /// <summary>Quality threshold for suggesting asset upgrades (0-100)</summary>
        public const string QualityUpgradeThreshold = "asset:quality:upgrade:threshold";
    }
}
