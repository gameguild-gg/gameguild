using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to partially update user metadata
/// </summary>
/// <param name="UserId">The user ID</param>
/// <param name="Request">The update request containing optional fields</param>
public sealed record UpdateUserMetadataCommand(Guid UserId, UpdateUserMetadataRequest Request) : ICommand;
