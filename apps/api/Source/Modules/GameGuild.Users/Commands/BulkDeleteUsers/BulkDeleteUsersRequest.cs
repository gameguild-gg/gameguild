namespace GameGuild.Users.Commands;

/// <summary>
///     Request model for bulk deleting users via API
/// </summary>
/// <param name="UserIds">Collection of user IDs to delete</param>
public record BulkDeleteUsersRequest(IEnumerable<Guid> UserIds);
