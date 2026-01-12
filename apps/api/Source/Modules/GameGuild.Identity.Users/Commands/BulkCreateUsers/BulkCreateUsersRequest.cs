namespace GameGuild.Identity.Users;

/// <summary>
///     Request model for bulk creating users via API
/// </summary>
/// <param name="Users">Collection of users to create</param>
public record BulkCreateUsersRequest(IEnumerable<CreateUserRequestItem> Users);
