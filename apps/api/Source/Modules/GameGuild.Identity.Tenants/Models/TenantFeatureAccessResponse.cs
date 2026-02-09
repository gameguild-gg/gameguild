namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response of a tenant feature access check
/// </summary>
public sealed record TenantFeatureAccessResponse
{
    /// <summary>
    ///     Whether the tenant has access to the feature
    /// </summary>
    public bool HasAccess { get; init; }

    /// <summary>
    ///     Reason for access denial (if any)
    /// </summary>
    public string? DenialReason { get; init; }

    /// <summary>
    ///     Feature key that was checked
    /// </summary>
    public string FeatureKey { get; init; } = string.Empty;

    /// <summary>
    ///     Additional metadata about the feature access
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    ///     Creates a successful access Response
    /// </summary>
    public static TenantFeatureAccessResponse Granted(string featureKey, Dictionary<string, object>? metadata = null)
    {
        return new TenantFeatureAccessResponse { HasAccess = true, FeatureKey = featureKey, Metadata = metadata };
    }

    /// <summary>
    ///     Creates a denied access Response
    /// </summary>
    public static TenantFeatureAccessResponse Denied(string featureKey, string reason, Dictionary<string, object>? metadata = null)
    {
        return new TenantFeatureAccessResponse { HasAccess = false, FeatureKey = featureKey, DenialReason = reason, Metadata = metadata };
    }
}
