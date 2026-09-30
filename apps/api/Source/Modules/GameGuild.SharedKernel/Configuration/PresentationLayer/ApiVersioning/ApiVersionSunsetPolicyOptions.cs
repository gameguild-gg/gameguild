namespace GameGuild.Configuration.PresentationLayer.ApiVersioning;

/// <summary>
///     Describes when an API version will be sunset and where clients can find the policy.
/// </summary>
public sealed class ApiVersionSunsetPolicyOptions
{
    /// <summary>
    ///     The UTC instant when the API version is no longer supported.
    /// </summary>
    public DateTimeOffset? EffectiveAt { get; set; }

    /// <summary>
    ///     An optional public HTTP or HTTPS policy URL.
    /// </summary>
    public string? PolicyUrl { get; set; }
}
