namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response for tenant deletion
/// </summary>
public sealed record DeleteTenantResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid TenantId { get; init; }
}
