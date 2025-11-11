using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to partially update user metadata
/// </summary>
/// <param name="UserId">The user ID</param>
/// <param name="Request">The update request containing optional fields</param>
public record UpdateUserMetadataCommand(Guid UserId, UpdateUserMetadataRequest Request) : ICommand;
