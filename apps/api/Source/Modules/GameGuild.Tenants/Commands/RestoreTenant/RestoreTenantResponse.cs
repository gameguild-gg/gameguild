namespace GameGuild.Tenants.Commands;

/// <summary>
///     Response for tenant restoration
/// </summary>
public record RestoreTenantResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid TenantId { get; init; }
}
