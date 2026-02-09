using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to update user localization preferences
/// </summary>
/// <param name="UserId">The user ID</param>
/// <param name="Request">Localization preferences to update</param>
public sealed record UpdateUserLocalizationPreferencesCommand(Guid UserId, UpdateUserLocalizationPreferencesRequest Request) : ICommand;
