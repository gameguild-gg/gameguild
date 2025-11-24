using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to update user preferences (partial update)
/// </summary>
/// <param name="UserId">The user ID</param>
/// <param name="Request">Preferences to update</param>
public record UpdateUserPreferencesCommand(Guid UserId, UpdateUserPreferencesRequest Request) : ICommand;
