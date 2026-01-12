using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to update multiple users
/// </summary>
/// <param name="Updates">Collection of user update data</param>
public record BulkUpdateUsersCommand(IEnumerable<UpdateUserRequestItem> Updates) : ICommand;
