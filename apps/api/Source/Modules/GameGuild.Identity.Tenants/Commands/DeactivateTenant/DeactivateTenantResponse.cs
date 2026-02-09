namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response for tenant deactivation
/// </summary>
public sealed record DeactivateTenantResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid TenantId { get; init; }
}
