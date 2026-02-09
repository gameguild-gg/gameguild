namespace GameGuild.Identity.Users;

/// <summary>
///     Request model for bulk deleting users via API
/// </summary>
/// <param name="UserIds">Collection of user IDs to delete</param>
public sealed record BulkDeleteUsersRequest(IEnumerable<Guid> UserIds);
