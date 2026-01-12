namespace GameGuild.Identity.Users;

/// <summary>
///     Request model for bulk unsuspending users via API
/// </summary>
/// <param name="UserIds">Collection of user IDs to unsuspend</param>
public record BulkUnsuspendUsersRequest(IEnumerable<Guid> UserIds);
