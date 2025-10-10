using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Queries;

/// <summary>
/// Query to check if a user has specific resource permission
/// </summary>
public class HasResourcePermissionQuery : IRequest<bool>
{
    /// <summary>
    /// User ID to check permission for
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Resource type
    /// </summary>
    public string ResourceType { get; init; } = string.Empty;

    /// <summary>
    /// Resource ID
    /// </summary>
    public Guid ResourceId { get; init; }

    /// <summary>
    /// Permission to check
    /// </summary>
    public PermissionType Permission { get; init; }
}
