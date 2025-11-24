namespace GameGuild.Tenants.Commands;

/// <summary>
///     Response for tenant activation
/// </summary>
public record ActivateTenantResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid TenantId { get; init; }
}
