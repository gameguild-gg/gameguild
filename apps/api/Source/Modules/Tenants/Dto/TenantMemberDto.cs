namespace GameGuild.Modules.Tenants;

/// <summary>
///     Data transfer object for tenant member
/// </summary>
public sealed record TenantMemberDto
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime JoinedAt { get; init; }
    public DateTime? LeftAt { get; init; }
    public string? LeaveReason { get; init; }
    public string? TenantName { get; init; }
    public string? TenantSlug { get; init; }
}
