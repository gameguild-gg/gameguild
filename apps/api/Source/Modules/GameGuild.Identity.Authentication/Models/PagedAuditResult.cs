namespace GameGuild.Identity.Authentication;

/// <summary>
///     Result for paginated audit log queries.
/// </summary>
/// <param name="Items">The audit entries</param>
/// <param name="TotalCount">Total count of entries</param>
public record PagedAuditResult(IEnumerable<ServiceAccountAuditEntry> Items, int TotalCount);
