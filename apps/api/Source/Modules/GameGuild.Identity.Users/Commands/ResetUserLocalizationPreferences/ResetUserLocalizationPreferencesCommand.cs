using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to reset user localization preferences to defaults
/// </summary>
/// <param name="UserId">The user ID</param>
public record ResetUserLocalizationPreferencesCommand(Guid UserId) : ICommand;
