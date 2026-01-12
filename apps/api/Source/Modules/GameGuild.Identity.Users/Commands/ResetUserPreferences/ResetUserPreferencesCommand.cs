using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to reset user preferences to defaults
/// </summary>
/// <param name="UserId">The user ID</param>
public record ResetUserPreferencesCommand(Guid UserId) : ICommand;
