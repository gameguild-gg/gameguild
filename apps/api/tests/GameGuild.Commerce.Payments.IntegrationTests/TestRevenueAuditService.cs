using System.Collections.Concurrent;

namespace GameGuild.Commerce.Payments.IntegrationTests;

internal sealed class TestRevenueAuditService : IRevenueAuditService
{
    public ConcurrentBag<RecordedAuditEntry> Entries { get; } = [];

    public Task RecordAuditTrailAsync(
        string entityType,
        Guid entityId,
        string action,
        Guid changedBy,
        string? oldValue = null,
        string? newValue = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        Entries.Add(new RecordedAuditEntry(entityType, entityId, action, changedBy, oldValue, newValue, reason));
        return Task.CompletedTask;
    }

    public Task<RevenueEvent> RecordRevenueEventAsync(RevenueEventType eventType, decimal amount, string currency, RevenueSource source, string referenceId, Guid? userId = null, string? metadata = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<RevenueEvent?> GetRevenueEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<List<RevenueEvent>> GetRevenueEventsByDateRangeAsync(DateTime startDate, DateTime endDate, int skip = 0, int take = 100, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<List<RevenueEvent>> GetRevenueEventsByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<FinancialLedgerEntry> CreateLedgerEntryAsync(LedgerEntryType entryType, string debitAccount, string creditAccount, decimal amount, string currency, string description, Guid? revenueEventId = null, string? referenceNumber = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<List<FinancialLedgerEntry>> GetLedgerEntriesByAccountAsync(string account, int skip = 0, int take = 100, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<List<FinancialLedgerEntry>> GetUnreconciledEntriesAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task ReconcileLedgerEntryAsync(Guid entryId, Guid reconciledBy, string? notes = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<List<AuditTrail>> GetAuditTrailByEntityAsync(string entityType, Guid entityId, int skip = 0, int take = 100, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

internal sealed record RecordedAuditEntry(
    string EntityType,
    Guid EntityId,
    string Action,
    Guid ChangedBy,
    string? OldValue,
    string? NewValue,
    string? Reason);
