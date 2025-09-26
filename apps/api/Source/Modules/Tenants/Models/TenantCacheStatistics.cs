namespace GameGuild.Modules.Tenants;

/// <summary>
/// Statistics about the tenant cache
/// </summary>
public record TenantCacheStatistics
{
    public int TenantCount { get; init; }

    public int TenantSettingsCount { get; init; }

    public int TenantDomainsCount { get; init; }

    public DateTime LastRefreshTime { get; init; }

    public bool IsInitialized { get; init; }
}
