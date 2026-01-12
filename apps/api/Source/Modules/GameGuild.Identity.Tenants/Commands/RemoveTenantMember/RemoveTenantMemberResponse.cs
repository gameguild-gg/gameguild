namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response for removing a tenant member
/// </summary>
public record RemoveTenantMemberResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }
}
