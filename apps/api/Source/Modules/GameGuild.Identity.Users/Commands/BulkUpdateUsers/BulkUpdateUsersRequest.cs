namespace GameGuild.Identity.Users;

/// <summary>
///     Request model for bulk updating users via API
/// </summary>
/// <param name="Updates">Collection of user updates</param>
public record BulkUpdateUsersRequest(IEnumerable<UpdateUserRequestItem> Updates);
