namespace GameGuild.Identity.Users;

/// <summary>
///     Request model for bulk activating users via API
/// </summary>
/// <param name="UserIds">Collection of user IDs to activate</param>
public sealed record BulkActivateUsersRequest(IEnumerable<Guid> UserIds);
