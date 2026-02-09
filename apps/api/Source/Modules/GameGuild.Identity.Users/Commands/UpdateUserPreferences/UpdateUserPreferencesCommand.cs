using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to update user preferences (partial update)
/// </summary>
/// <param name="UserId">The user ID</param>
/// <param name="Request">Preferences to update</param>
public sealed record UpdateUserPreferencesCommand(Guid UserId, UpdateUserPreferencesRequest Request) : ICommand;
