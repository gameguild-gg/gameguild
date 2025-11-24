namespace GameGuild.Tenants.Commands;

/// <summary>
///     Response for tenant deletion
/// </summary>
public record DeleteTenantResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid TenantId { get; init; }
}
