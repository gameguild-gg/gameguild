namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response for removing a tenant member
/// </summary>
public sealed record RemoveTenantMemberResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }
}
