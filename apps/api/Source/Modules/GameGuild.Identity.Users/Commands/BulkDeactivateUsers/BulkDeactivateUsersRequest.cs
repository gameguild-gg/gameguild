namespace GameGuild.Identity.Users;

/// <summary>
///     Request model for bulk deactivating users via API
/// </summary>
/// <param name="UserIds">Collection of user IDs to deactivate</param>
public sealed record BulkDeactivateUsersRequest(IEnumerable<Guid> UserIds);
