using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Queries;

/// <summary>
/// Query to check if a user has specific global permission
/// </summary>
public class HasGlobalPermissionQuery : IRequest<bool>
{
    /// <summary>
    /// User ID to check permission for
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Permission to check
    /// </summary>
    public PermissionType Permission { get; init; }
}
