using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to update user localization preferences
/// </summary>
/// <param name="UserId">The user ID</param>
/// <param name="Request">Localization preferences to update</param>
public record UpdateUserLocalizationPreferencesCommand(Guid UserId, UpdateUserLocalizationPreferencesRequest Request) : ICommand;
