namespace GameGuild.Tenants.Commands;

/// <summary>
///     Response for tenant deactivation
/// </summary>
public record DeactivateTenantResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid TenantId { get; init; }
}
