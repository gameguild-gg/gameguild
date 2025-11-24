using GameGuild.CQRS;

namespace GameGuild.Permissions.Application.Commands.GrantTenantPermission;

/// <summary>
///     Command to grant tenant-level permissions to a user.
/// </summary>
public sealed record GrantTenantPermissionCommand : ICommand<Guid>
{
    /// <summary>
    ///     Gets the tenant ID.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the user ID to grant permissions to.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Gets the permissions to grant.
    /// </summary>
    public required string[ ] Permissions { get; init; }

    /// <summary>
    ///     Gets the ID of the user granting the permissions.
    /// </summary>
    public required Guid GrantedBy { get; init; }

    /// <summary>
    ///     Gets the optional expiration date for the permissions.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    ///     Gets the optional reason for granting permissions.
    /// </summary>
    public string? Reason { get; init; }
}
