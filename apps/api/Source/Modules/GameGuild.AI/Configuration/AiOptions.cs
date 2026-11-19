namespace GameGuild.AI;

/// <summary>
///     Platform-level configuration for AI backends and defaults.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "AI";

    public bool Enabled { get; set; }

    public string? DefaultBackend { get; set; }

    public bool AllowTenantOverrides { get; set; } = true;

    public Dictionary<string, AiBackendOptions> Backends { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
///     Platform-level configuration for a named AI backend.
/// </summary>
public sealed class AiBackendOptions
{
    public string? Adapter { get; set; }

    public string? ApiKey { get; set; }

    public string? BaseUrl { get; set; }

    public string? DefaultModel { get; set; }
}
