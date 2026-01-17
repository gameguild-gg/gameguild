using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to revoke a resource permission grant by its ID
/// </summary>
public record RevokeResourcePermissionByIdCommand : ICommand
{
    /// <summary>
    ///     The unique identifier of the permission grant to revoke
    /// </summary>
    public required Guid GrantId { get; init; }
}
