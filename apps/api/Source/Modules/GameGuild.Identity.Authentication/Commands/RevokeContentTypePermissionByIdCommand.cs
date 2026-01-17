using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to revoke a content type permission grant by its ID
/// </summary>
public record RevokeContentTypePermissionByIdCommand : ICommand
{
    /// <summary>
    ///     The unique identifier of the permission grant to revoke
    /// </summary>
    public required Guid GrantId { get; init; }
}
