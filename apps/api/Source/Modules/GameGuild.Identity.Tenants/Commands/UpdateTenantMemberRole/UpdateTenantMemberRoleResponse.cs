namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response for updating tenant member role
/// </summary>
public record UpdateTenantMemberRoleResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid MemberId { get; init; }

    public string NewRole { get; init; } = string.Empty;
}
