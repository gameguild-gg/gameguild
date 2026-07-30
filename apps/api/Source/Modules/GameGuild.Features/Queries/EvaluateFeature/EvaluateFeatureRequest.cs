namespace GameGuild.Features;

/// <summary>
///     Request DTO for feature evaluation via HTTP API
/// </summary>
public sealed class EvaluateFeatureRequest
{
    /// <summary>
    ///     The feature flag key to evaluate
    /// </summary>
    public required string FeatureKey { get; set; }

    /// <summary>
    ///     Optional user ID (defaults to current user)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     Optional tenant ID (defaults to current tenant)
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     Optional environment (defaults to "production")
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    ///     Optional user permissions (defaults to current user's permissions)
    /// </summary>
    public List<string>? Permissions { get; set; }

    /// <summary>
    ///     Optional custom attributes for targeting
    /// </summary>
    public Dictionary<string, object>? CustomAttributes { get; set; }
}
