namespace GameGuild.Users.Commands;

/// <summary>
///     Request model for bulk activating users via API
/// </summary>
/// <param name="UserIds">Collection of user IDs to activate</param>
public record BulkActivateUsersRequest(IEnumerable<Guid> UserIds);
