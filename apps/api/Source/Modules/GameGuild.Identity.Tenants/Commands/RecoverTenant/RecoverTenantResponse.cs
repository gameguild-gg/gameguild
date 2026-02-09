namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response for tenant recovery operation
/// </summary>
public sealed record RecoverTenantResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid TenantId { get; init; }
}
