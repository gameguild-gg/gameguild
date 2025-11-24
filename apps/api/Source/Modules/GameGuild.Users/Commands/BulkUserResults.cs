namespace GameGuild.Users.Commands;

/// <summary>
///     Result of bulk create users operation
/// </summary>
/// <param name="CreatedUserIds">Successfully created user IDs</param>
/// <param name="FailedEmails">Email addresses that failed to create</param>
public record BulkCreateUsersResult(IEnumerable<Guid> CreatedUserIds, IEnumerable<string> FailedEmails);
