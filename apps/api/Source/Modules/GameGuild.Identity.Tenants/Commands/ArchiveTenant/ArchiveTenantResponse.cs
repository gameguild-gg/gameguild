namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response for tenant archival
/// </summary>
public record ArchiveTenantResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid TenantId { get; init; }
}
