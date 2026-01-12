namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response for tenant deletion
/// </summary>
public record DeleteTenantResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid TenantId { get; init; }
}
