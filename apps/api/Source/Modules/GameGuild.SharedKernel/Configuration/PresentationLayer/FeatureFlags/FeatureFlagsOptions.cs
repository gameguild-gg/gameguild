namespace GameGuild.Configuration.PresentationLayer.FeatureFlags;

public sealed class FeatureFlagsOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "FeatureFlags";

    public bool EnableOpenFeature { get; set; } = false;

    public string? Provider { get; set; }

    public static FeatureFlagsOptions CreateDefault() { return new FeatureFlagsOptions(); }
}
