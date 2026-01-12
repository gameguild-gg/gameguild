namespace GameGuild.Identity.Users;

/// <summary>
///     Request model for bulk suspending users via API
/// </summary>
/// <param name="UserIds">Collection of user IDs to suspend</param>
public record BulkSuspendUsersRequest(IEnumerable<Guid> UserIds);
