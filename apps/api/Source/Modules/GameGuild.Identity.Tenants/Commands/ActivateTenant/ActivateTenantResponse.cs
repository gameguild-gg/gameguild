namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response for tenant activation
/// </summary>
public sealed record ActivateTenantResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid TenantId { get; init; }
}
