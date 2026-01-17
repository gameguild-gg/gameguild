using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to revoke a tenant permission grant by its ID
/// </summary>
public record RevokeTenantPermissionByIdCommand : ICommand
{
    /// <summary>
    ///     The unique identifier of the permission grant to revoke
    /// </summary>
    public required Guid GrantId { get; init; }
}
