namespace GameGuild.Configuration.PresentationLayer.Authorization;

/// <summary>
/// Configuration options for multi-tenant resolution.
/// </summary>
public sealed class TenancyOptions : BaseOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Tenancy";

    /// <summary>
    /// The default tenant identifier when no tenant can be resolved.
    /// </summary>
    public string DefaultTenantId { get; set; } = "base";

    /// <summary>
    /// Resolution options for determining the current tenant.
    /// </summary>
    public TenancyResolutionOptions Resolution { get; set; } = new();

    /// <summary>
    /// Fallback options when tenant cannot be resolved from request.
    /// </summary>
    public TenancyFallbackOptions Fallback { get; set; } = new();

    /// <inheritdoc />
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(DefaultTenantId))
        {
            throw new InvalidOperationException("DefaultTenantId cannot be null or empty.");
        }
    }

    /// <summary>
    /// Creates a default instance of <see cref="TenancyOptions"/>.
    /// </summary>
    public static TenancyOptions CreateDefault() => new();
}

/// <summary>
/// Options for tenant resolution strategies.
/// </summary>
public sealed class TenancyResolutionOptions
{
    /// <summary>
    /// Enables tenant resolution from an HTTP header.
    /// </summary>
    public bool EnableHeader { get; set; } = true;

    /// <summary>
    /// The header name used to read the tenant identifier.
    /// </summary>
    public string HeaderName { get; set; } = "X-Tenant-Id";

    /// <summary>
    /// Enables tenant resolution from a subdomain.
    /// </summary>
    public bool EnableSubdomain { get; set; } = true;

    /// <summary>
    /// Subdomains ignored during resolution.
    /// </summary>
    public List<string> SubdomainIgnoreList { get; set; } = ["api", "www", "admin", "staging"];

    /// <summary>
    /// Enables tenant resolution from the query string.
    /// </summary>
    public bool EnableQueryString { get; set; }

    /// <summary>
    /// The query-string key used to read the tenant identifier.
    /// </summary>
    public string QueryStringKey { get; set; } = "tenantId";
}

/// <summary>
/// Options for fallback tenant resolution.
/// </summary>
public sealed class TenancyFallbackOptions
{
    /// <summary>
    /// Fallback mode used when request-based resolution fails.
    /// </summary>
    public string Mode { get; set; } = "UserDefaultThenBase";
}
