using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to replace user localization preferences
/// </summary>
/// <param name="UserId">The user ID</param>
/// <param name="Request">Complete set of localization preferences</param>
public record ReplaceUserLocalizationPreferencesCommand(Guid UserId, ReplaceUserLocalizationPreferencesRequest Request) : ICommand;
