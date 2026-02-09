namespace GameGuild.Identity.Users;

/// <summary>
///     Response of bulk create users operation
/// </summary>
/// <param name="CreatedUserIds">Successfully created user IDs</param>
/// <param name="FailedEmails">Email addresses that failed to create</param>
public sealed record BulkCreateUsersResponse(IEnumerable<Guid> CreatedUserIds, IEnumerable<string> FailedEmails);
