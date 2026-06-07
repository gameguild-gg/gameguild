namespace GameGuild.AI;

/// <summary>
///     Platform-level configuration for AI providers and defaults.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "AI";

    public bool Enabled { get; set; }

    public string? DefaultProvider { get; set; }

    public bool AllowTenantOverrides { get; set; } = true;

    public Dictionary<string, AiProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
///     Platform-level configuration for a specific AI provider.
/// </summary>
public sealed class AiProviderOptions
{
    public string? ApiKey { get; set; }

    public string? BaseUrl { get; set; }

    public string? DefaultModel { get; set; }
}