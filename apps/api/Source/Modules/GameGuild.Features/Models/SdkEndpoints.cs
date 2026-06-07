namespace GameGuild.Features;

/// <summary>
///     SDK endpoint configuration
/// </summary>
public class SdkEndpoints
{
    /// <summary>
    ///     Endpoint for fetching feature flags
    /// </summary>
    public string Features { get; set; } = "/features";

    /// <summary>
    ///     Endpoint for evaluating feature flags
    /// </summary>
    public string Evaluate { get; set; } = "/features/evaluate";

    /// <summary>
    ///     Endpoint for submitting analytics
    /// </summary>
    public string Analytics { get; set; } = "/features/analytics";

    /// <summary>
    ///     Endpoint for health checks
    /// </summary>
    public string Health { get; set; } = "/health";

    /// <summary>
    ///     Endpoint for SDK configuration
    /// </summary>
    public string Config { get; set; } = "/sdk/config";
}
