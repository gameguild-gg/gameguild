namespace GameGuild.Users.Commands;

/// <summary>
///     Request model for bulk deactivating users via API
/// </summary>
/// <param name="UserIds">Collection of user IDs to deactivate</param>
public record BulkDeactivateUsersRequest(IEnumerable<Guid> UserIds);
