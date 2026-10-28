namespace GameGuild.Commerce.Payments;

/// <summary>
///     Tenant-scoped fraud blacklist management and evaluation (issue #409). Entries support
///     wildcard patterns, expiry dates and manual whitelist overrides; evaluation is used both
///     by the payment fraud gate (as a signal provider) and by the admin check API.
/// </summary>
public interface IFraudBlacklistService
{
    /// <summary>
    ///     Evaluates a concrete value against the tenant's active entries of one type. An
    ///     active whitelist override suppresses blacklist matches for the same value.
    /// </summary>
    /// <param name="tenantId">Tenant whose entries are evaluated.</param>
    /// <param name="entryType">Entry kind to evaluate.</param>
    /// <param name="value">Concrete value (e.g. a fingerprint, token or domain).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation result; <see cref="FraudBlacklistEvaluation.NoMatch"/> when nothing matched.</returns>
    Task<FraudBlacklistMatch> EvaluateAsync(
        Guid tenantId,
        FraudBlacklistEntryType entryType,
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds an entry idempotently: when an identical live entry (same tenant, type,
    ///     normalized value and kind) already exists it is returned unchanged.
    /// </summary>
    /// <param name="entry">Entry to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted entry.</returns>
    Task<FraudBlacklistEntry> AddAsync(FraudBlacklistEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Soft-deletes an entry owned by the tenant. Unknown ids are a no-op.
    /// </summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="entryId">Entry to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when an entry was removed.</returns>
    Task<bool> RemoveAsync(Guid tenantId, Guid entryId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Lists the tenant's entries with optional filters, newest first.
    /// </summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="entryType">Optional entry-type filter.</param>
    /// <param name="includeExpired">When false, entries whose expiry has passed are omitted.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size (1-100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching page plus the total count for the filter.</returns>
    Task<(IReadOnlyList<FraudBlacklistEntry> Items, int TotalCount)> ListAsync(
        Guid tenantId,
        FraudBlacklistEntryType? entryType = null,
        bool includeExpired = false,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
