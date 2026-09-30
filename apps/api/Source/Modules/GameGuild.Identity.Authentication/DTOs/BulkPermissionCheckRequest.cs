using GameGuild.Identity.Authorization;

namespace GameGuild.Identity.Authentication;

/// <summary>
/// Describes one permission decision in a bulk authorization operation.
/// </summary>
public sealed record BulkPermissionCheckRequest(
    Guid UserId,
    Guid? TenantId,
    PermissionType Permission,
    string? ContentTypeName = null,
    Guid? ResourceId = null,
    string? ResourceTypeName = null);
