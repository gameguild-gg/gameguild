namespace GameGuild.Tenants.Commands;

/// <summary>
///     Response for tenant recovery operation
/// </summary>
public record RecoverTenantResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid TenantId { get; init; }
}
