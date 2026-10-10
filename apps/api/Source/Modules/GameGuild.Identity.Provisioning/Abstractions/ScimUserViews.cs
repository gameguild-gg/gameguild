namespace GameGuild.Identity.Provisioning;

/// <summary>
///     Flattened, read-side projection of a provisioned user used by the SCIM list and
///     filter surface. ExternalId is null when a platform user has no SCIM mapping
///     (unmapped users are never returned by the SCIM surface; the projection keeps the
///     join shape simple).
/// </summary>
public sealed record ScimUserView(
    Guid UserId,
    string? ExternalId,
    string? UserName,
    string Email,
    string DisplayName,
    string? PhoneNumber,
    bool Active,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
///     Flattened, read-side projection of a provisioned group (tenant role).
/// </summary>
public sealed record ScimGroupView(
    Guid RoleId,
    string? ExternalId,
    string DisplayName,
    string? Description,
    bool Active,
    DateTime CreatedAt,
    DateTime UpdatedAt);
