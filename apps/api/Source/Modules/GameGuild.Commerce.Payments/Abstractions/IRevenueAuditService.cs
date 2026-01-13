namespace GameGuild.Commerce.Payments;

/// <summary>
///     Service for managing revenue events and financial ledger
/// </summary>
public interface IRevenueAuditService
{
    /// <summary>Record a revenue event</summary>
    Task<RevenueEvent> RecordRevenueEventAsync(
        RevenueEventType eventType,
        decimal amount,
        string currency,
        RevenueSource source,
        string referenceId,
        Guid? userId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Get revenue event by ID</summary>
    Task<RevenueEvent?> GetRevenueEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Get revenue events by date range</summary>
    Task<List<RevenueEvent>> GetRevenueEventsByDateRangeAsync(DateTime startDate, DateTime endDate, int skip = 0, int take = 100, CancellationToken cancellationToken = default);

    /// <summary>Get revenue events by reference ID</summary>
    Task<List<RevenueEvent>> GetRevenueEventsByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);

    /// <summary>Create a ledger entry</summary>
    Task<FinancialLedgerEntry> CreateLedgerEntryAsync(
        LedgerEntryType entryType,
        string debitAccount,
        string creditAccount,
        decimal amount,
        string currency,
        string description,
        Guid? revenueEventId = null,
        string? referenceNumber = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Get ledger entries by account</summary>
    Task<List<FinancialLedgerEntry>> GetLedgerEntriesByAccountAsync(string account, int skip = 0, int take = 100, CancellationToken cancellationToken = default);

    /// <summary>Get unreconciled ledger entries</summary>
    Task<List<FinancialLedgerEntry>> GetUnreconciledEntriesAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);

    /// <summary>Reconcile a ledger entry</summary>
    Task ReconcileLedgerEntryAsync(Guid entryId, Guid reconciledBy, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>Record an audit trail entry</summary>
    Task RecordAuditTrailAsync(
        string entityType,
        Guid entityId,
        string action,
        Guid changedBy,
        string? oldValue = null,
        string? newValue = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? reason = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Get audit trail by entity</summary>
    Task<List<AuditTrail>> GetAuditTrailByEntityAsync(string entityType, Guid entityId, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
}
