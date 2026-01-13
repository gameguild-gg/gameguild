namespace GameGuild.Commerce.Payments;

/// <summary>
///     Repository for audit trail entries
/// </summary>
public interface IAuditTrailRepository
{
    /// <summary>Get audit trail entries by entity</summary>
    Task<List<AuditTrail>> GetByEntityAsync(string entityType, Guid entityId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Add new audit trail entry</summary>
    Task AddAsync(AuditTrail auditTrail, CancellationToken cancellationToken = default);

    /// <summary>Save changes to database</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
