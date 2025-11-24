using GameGuild.CQRS;

namespace GameGuild.Permissions.Application.Commands.RevokeTenantPermission;

/// <summary>
///     Command to revoke tenant-level permissions from a user.
/// </summary>
public sealed record RevokeTenantPermissionCommand : ICommand<bool>
{
    /// <summary>
    ///     Gets the tenant ID.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the user ID to revoke permissions from.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Gets the permissions to revoke.
    /// </summary>
    public required string[ ] Permissions { get; init; }

    /// <summary>
    ///     Gets the ID of the user revoking the permissions.
    /// </summary>
    public required Guid RevokedBy { get; init; }

    /// <summary>
    ///     Gets the optional reason for revoking permissions.
    /// </summary>
    public string? Reason { get; init; }
}
