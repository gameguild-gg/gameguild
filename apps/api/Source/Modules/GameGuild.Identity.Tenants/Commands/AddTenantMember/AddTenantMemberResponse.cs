namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response for adding a tenant member
/// </summary>
public sealed record AddTenantMemberResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid? MemberId { get; init; }
}
