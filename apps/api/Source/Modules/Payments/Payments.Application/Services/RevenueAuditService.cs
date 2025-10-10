using GameGuild.Modules.Payments.Payments.Application.Repositories;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Payments.Payments.Application.Services;

public class RevenueAuditService : IRevenueAuditService
{
    private readonly IRevenueEventRepository _revenueEventRepository;
    private readonly IFinancialLedgerRepository _ledgerRepository;
    private readonly IAuditTrailRepository _auditTrailRepository;
    private readonly ILogger<RevenueAuditService> _logger;

    public RevenueAuditService(
        IRevenueEventRepository revenueEventRepository,
        IFinancialLedgerRepository ledgerRepository,
        IAuditTrailRepository auditTrailRepository,
        ILogger<RevenueAuditService> logger)
    {
        _revenueEventRepository = revenueEventRepository;
        _ledgerRepository = ledgerRepository;
        _auditTrailRepository = auditTrailRepository;
        _logger = logger;
    }

    public async Task<RevenueEvent> RecordRevenueEventAsync(
        RevenueEventType eventType,
        decimal amount,
        string currency,
        RevenueSource source,
        string referenceId,
        Guid? userId = null,
        Guid? tenantId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recording revenue event: {EventType} for amount {Amount} {Currency}", eventType, amount, currency);

        var revenueEvent = new RevenueEvent
        {
            EventType = eventType,
            Amount = amount,
            Currency = currency,
            Source = source,
            ReferenceId = referenceId,
            Timestamp = DateTime.UtcNow,
            Status = RevenueEventStatus.Pending,
            UserId = userId,
            TenantId = tenantId,
            Metadata = metadata
        };

        await _revenueEventRepository.AddAsync(revenueEvent, cancellationToken);
        await _revenueEventRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Revenue event recorded with ID: {EventId}", revenueEvent.Id);
        return revenueEvent;
    }

    public async Task<RevenueEvent?> GetRevenueEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _revenueEventRepository.GetByIdAsync(eventId, cancellationToken);
    }

    public async Task<List<RevenueEvent>> GetRevenueEventsByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving revenue events from {StartDate} to {EndDate}", startDate, endDate);
        return await _revenueEventRepository.GetByDateRangeAsync(startDate, endDate, skip, take, cancellationToken);
    }

    public async Task<List<RevenueEvent>> GetRevenueEventsByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        return await _revenueEventRepository.GetByReferenceIdAsync(referenceId, cancellationToken);
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
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating ledger entry: {EntryType} - Debit: {DebitAccount}, Credit: {CreditAccount}, Amount: {Amount}",
            entryType, debitAccount, creditAccount, amount);

        var entry = new FinancialLedgerEntry
        {
            EntryType = entryType,
            DebitAccount = debitAccount,
            CreditAccount = creditAccount,
            Amount = amount,
            Currency = currency,
            Description = description,
            RevenueEventId = revenueEventId,
            ReferenceNumber = referenceNumber ?? Guid.NewGuid().ToString(),
            IsReconciled = false,
            FiscalYear = DateTime.UtcNow.Year,
            FiscalPeriod = DateTime.UtcNow.Month
        };

        await _ledgerRepository.AddAsync(entry, cancellationToken);
        await _ledgerRepository.SaveChangesAsync(cancellationToken);

        // Mark revenue event as processed if linked
        if (revenueEventId.HasValue)
        {
            var revenueEvent = await _revenueEventRepository.GetByIdAsync(revenueEventId.Value, cancellationToken);
            if (revenueEvent != null)
            {
                revenueEvent.MarkAsProcessed(entry.Id);
                await _revenueEventRepository.UpdateAsync(revenueEvent, cancellationToken);
                await _revenueEventRepository.SaveChangesAsync(cancellationToken);
            }
        }

        _logger.LogInformation("Ledger entry created with ID: {EntryId}", entry.Id);
        return entry;
    }

    public async Task<List<FinancialLedgerEntry>> GetLedgerEntriesByAccountAsync(
        string account,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving ledger entries for account: {Account}", account);
        return await _ledgerRepository.GetByAccountAsync(account, skip, take, cancellationToken);
    }

    public async Task<List<FinancialLedgerEntry>> GetUnreconciledEntriesAsync(
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _ledgerRepository.GetUnreconciledAsync(skip, take, cancellationToken);
    }

    public async Task ReconcileLedgerEntryAsync(Guid entryId, Guid reconciledBy, string? notes = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reconciling ledger entry: {EntryId} by user: {UserId}", entryId, reconciledBy);

        var entry = await _ledgerRepository.GetByIdAsync(entryId, cancellationToken);
        if (entry == null)
        {
            throw new InvalidOperationException($"Ledger entry with ID {entryId} not found");
        }

        entry.Reconcile(reconciledBy, notes);
        await _ledgerRepository.UpdateAsync(entry, cancellationToken);
        await _ledgerRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ledger entry {EntryId} reconciled successfully", entryId);
    }

    public async Task<AuditTrail> RecordAuditTrailAsync(
        string entityType,
        Guid entityId,
        AuditAction action,
        Guid changedBy,
        string? oldValue = null,
        string? newValue = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? reason = null,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recording audit trail: {Action} on {EntityType} {EntityId}", action, entityType, entityId);

        var auditTrail = new AuditTrail
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Reason = reason,
            TenantId = tenantId
        };

        await _auditTrailRepository.AddAsync(auditTrail, cancellationToken);
        await _auditTrailRepository.SaveChangesAsync(cancellationToken);

        return auditTrail;
    }

    public async Task<List<AuditTrail>> GetAuditTrailByEntityAsync(
        string entityType,
        Guid entityId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving audit trail for {EntityType} {EntityId}", entityType, entityId);
        return await _auditTrailRepository.GetByEntityAsync(entityType, entityId, skip, take, cancellationToken);
    }
}
