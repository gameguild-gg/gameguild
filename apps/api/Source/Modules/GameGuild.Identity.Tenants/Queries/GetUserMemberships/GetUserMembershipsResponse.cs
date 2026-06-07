namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response containing all tenant memberships for a user.
/// </summary>
public sealed record GetUserMembershipsResponse
{
    /// <summary>
    ///     List of tenant memberships the user belongs to
    /// </summary>
    public IReadOnlyList<UserMembershipDto> Memberships { get; init; } = [];

    /// <summary>
    ///     Total count of memberships
    /// </summary>
    public int TotalCount { get; init; }
}

/// <summary>
///     DTO representing a user's membership in a tenant.
///     Provides tenant information along with the user's role and status.
/// </summary>
public sealed record UserMembershipDto
{
    /// <summary>
    ///     The membership ID
    /// </summary>
    public Guid MembershipId { get; init; }

    /// <summary>
    ///     The tenant ID
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    ///     The tenant name
    /// </summary>
    public string TenantName { get; init; } = string.Empty;

    /// <summary>
    ///     The tenant slug (URL-friendly identifier)
    /// </summary>
    public string TenantSlug { get; init; } = string.Empty;

    /// <summary>
    ///     Whether the tenant is currently active.
    /// </summary>
    public bool TenantIsActive { get; init; }

    /// <summary>
    ///     Optional tenant description
    /// </summary>
    public string? TenantDescription { get; init; }

    /// <summary>
    ///     The user's role within this tenant
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    ///     Whether this membership is currently active
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    ///     When the user joined this tenant
    /// </summary>
    public DateTime JoinedAt { get; init; }

    /// <summary>
    ///     When the user left this tenant (null if still a member)
    /// </summary>
    public DateTime? LeftAt { get; init; }
}
