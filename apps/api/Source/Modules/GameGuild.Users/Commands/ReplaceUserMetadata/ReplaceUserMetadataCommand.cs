using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to completely replace user metadata
/// </summary>
/// <param name="UserId">The user ID</param>
/// <param name="Request">The replacement request containing all fields</param>
public record ReplaceUserMetadataCommand(Guid UserId, ReplaceUserMetadataRequest Request) : ICommand;
