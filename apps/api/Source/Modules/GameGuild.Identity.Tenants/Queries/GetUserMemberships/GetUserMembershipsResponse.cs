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

    /// <summary>
    ///     Invitation workflow status, when this membership was created as an invite.
    /// </summary>
    public string? InviteStatus { get; init; }

    /// <summary>
    ///     Email of the operator who sent the invite.
    /// </summary>
    public string? InvitedByEmail { get; init; }

    /// <summary>
    ///     Email of the user who received the invite.
    /// </summary>
    public string? InviteeEmail { get; init; }

    /// <summary>
    ///     Display name of the user who received the invite.
    /// </summary>
    public string? InviteeName { get; init; }

    /// <summary>
    ///     When the invite was created.
    /// </summary>
    public DateTime? InvitedAt { get; init; }

    /// <summary>
    ///     When the invite was last sent.
    /// </summary>
    public DateTime? LastInviteSentAt { get; init; }

    /// <summary>
    ///     When the invite was accepted.
    /// </summary>
    public DateTime? AcceptedAt { get; init; }

    /// <summary>
    ///     When the invite was cancelled.
    /// </summary>
    public DateTime? CancelledAt { get; init; }

    /// <summary>
    ///     Number of times the invite has been sent.
    /// </summary>
    public int InviteResendCount { get; init; }
}
