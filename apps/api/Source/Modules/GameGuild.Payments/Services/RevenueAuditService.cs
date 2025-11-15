using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;
using Microsoft.Extensions.Logging;

namespace GameGuild.Payments.Services;

/// <summary>
///     Revenue and audit service implementation
/// </summary>
public class RevenueAuditService(
    IRevenueEventRepository revenueEventRepository,
    IFinancialLedgerRepository ledgerRepository,
    IAuditTrailRepository auditTrailRepository,
    ILogger<RevenueAuditService> logger
) : IRevenueAuditService
{
    public async Task<RevenueEvent> RecordRevenueEventAsync(
        RevenueEventType eventType,
        decimal amount,
        string currency,
        RevenueSource source,
        string referenceId,
        Guid? userId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("Recording revenue event: {EventType} for amount {Amount} {Currency}", eventType, amount, currency);

        var revenueEvent = new RevenueEvent { EventType = eventType, Amount = amount, Currency = currency, Source = source, ReferenceId = referenceId, Timestamp = DateTime.UtcNow, UserId = userId, Metadata = metadata };

        await revenueEventRepository.AddAsync(revenueEvent, cancellationToken).ConfigureAwait(false);
        await revenueEventRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Revenue event recorded with ID: {EventId}", revenueEvent.Id);

        return revenueEvent;
    }

    public async Task<RevenueEvent?> GetRevenueEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await revenueEventRepository.GetByIdAsync(eventId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<RevenueEvent>> GetRevenueEventsByDateRangeAsync(DateTime startDate, DateTime endDate, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving revenue events from {StartDate} to {EndDate}", startDate, endDate);

        return await revenueEventRepository.GetByDateRangeAsync(startDate, endDate, skip, take, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<RevenueEvent>> GetRevenueEventsByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        return await revenueEventRepository.GetByReferenceIdAsync(referenceId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FinancialLedgerEntry> CreateLedgerEntryAsync(
        LedgerEntryType entryType,
        string debitAccount,
        string creditAccount,
        decimal amount,
        string currency,
        string description,
        Guid? revenueEventId = null,
        string? referenceNumber = null,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("Creating ledger entry: {EntryType} - Debit: {DebitAccount}, Credit: {CreditAccount}, Amount: {Amount}", entryType, debitAccount, creditAccount, amount);

        var entry = new FinancialLedgerEntry
        {
            EntryType = entryType,
            DebitAccount = debitAccount,
            CreditAccount = creditAccount,
            Amount = amount,
            Currency = currency,
            Description = description,
            ReferenceNumber = referenceNumber ?? Guid.NewGuid().ToString(),
            IsReconciled = false,
            FiscalYear = DateTime.UtcNow.Year,
            FiscalPeriod = DateTime.UtcNow.Month
        };

        await ledgerRepository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        await ledgerRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Mark revenue event as processed if linked
        // The relationship is configured with RevenueEvent.LedgerEntryId as FK
        if (revenueEventId.HasValue)
        {
            var revenueEvent = await revenueEventRepository.GetByIdAsync(revenueEventId.Value, cancellationToken).ConfigureAwait(false);
            if (revenueEvent != null)
            {
                revenueEvent.MarkAsProcessed(entry.Id);
                await revenueEventRepository.UpdateAsync(revenueEvent, cancellationToken).ConfigureAwait(false);
                await revenueEventRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        logger.LogInformation("Ledger entry created with ID: {EntryId}", entry.Id);

        return entry;
    }

    public async Task<List<FinancialLedgerEntry>> GetLedgerEntriesByAccountAsync(string account, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving ledger entries for account: {Account}", account);

        return await ledgerRepository.GetByAccountAsync(account, skip, take, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<FinancialLedgerEntry>> GetUnreconciledEntriesAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        return await ledgerRepository.GetUnreconciledAsync(skip, take, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReconcileLedgerEntryAsync(Guid entryId, Guid reconciledBy, string? notes = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reconciling ledger entry: {EntryId} by user: {UserId}", entryId, reconciledBy);

        var entry = await ledgerRepository.GetByIdAsync(entryId, cancellationToken).ConfigureAwait(false);

        if (entry == null) { throw new InvalidOperationException($"Ledger entry with ID {entryId} not found"); }

        entry.Reconcile(reconciledBy, notes);
        await ledgerRepository.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        await ledgerRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Ledger entry {EntryId} reconciled successfully", entryId);
    }

    public async Task RecordAuditTrailAsync(
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
    )
    {
        logger.LogInformation("Recording audit trail: {Action} on {EntityType} {EntityId}", action, entityType, entityId);

        // Parse action string to AuditAction enum with fallback to Other
        if (!Enum.TryParse(action, true, out AuditAction auditAction)) { auditAction = AuditAction.Other; }

        var auditTrail = new AuditTrail
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = auditAction,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Reason = reason
        };

        await auditTrailRepository.AddAsync(auditTrail, cancellationToken).ConfigureAwait(false);
        await auditTrailRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<AuditTrail>> GetAuditTrailByEntityAsync(string entityType, Guid entityId, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving audit trail for {EntityType} {EntityId}", entityType, entityId);

        return await auditTrailRepository.GetByEntityAsync(entityType, entityId, skip, take, cancellationToken).ConfigureAwait(false);
    }
}
