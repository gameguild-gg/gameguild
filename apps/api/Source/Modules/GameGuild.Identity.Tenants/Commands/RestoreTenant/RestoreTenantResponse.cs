namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response for tenant restoration
/// </summary>
public sealed record RestoreTenantResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid TenantId { get; init; }
}
