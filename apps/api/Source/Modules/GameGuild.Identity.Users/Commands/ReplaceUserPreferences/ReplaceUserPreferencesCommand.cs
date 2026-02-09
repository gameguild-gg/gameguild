using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to replace user preferences (full update)
/// </summary>
/// <param name="UserId">The user ID</param>
/// <param name="Request">Complete set of preferences</param>
public sealed record ReplaceUserPreferencesCommand(Guid UserId, ReplaceUserPreferencesRequest Request) : ICommand;
