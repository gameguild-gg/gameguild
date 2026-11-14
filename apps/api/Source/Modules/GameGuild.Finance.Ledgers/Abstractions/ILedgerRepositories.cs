using GameGuild.Finance.Ledgers.Entities;
using GameGuild.Finance.Ledgers.Enums;

namespace GameGuild.Finance.Ledgers.Abstractions;

/// <summary>
/// Repository for Ledger aggregate root operations.
/// </summary>
public interface ILedgerRepository
{
    // ========================================================================
    // Basic CRUD
    // ========================================================================

    Task<Ledger?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Ledger?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct = default);
    Task<Ledger?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Ledger>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Ledger>> GetRootLedgersAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Ledger>> GetChildrenAsync(Guid parentLedgerId, CancellationToken ct = default);
    Task<IReadOnlyList<Ledger>> GetVirtualLedgersAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid tenantId, string code, CancellationToken ct = default);
    Task AddAsync(Ledger ledger, CancellationToken ct = default);
    Task UpdateAsync(Ledger ledger, CancellationToken ct = default);
    Task DeleteAsync(Ledger ledger, CancellationToken ct = default);
}

/// <summary>
/// Repository for LedgerClosure (hierarchy) operations.
/// </summary>
public interface ILedgerClosureRepository
{
    // ========================================================================
    // Closure Management
    // ========================================================================

    Task AddAsync(LedgerClosure closure, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<LedgerClosure> closures, CancellationToken ct = default);
    Task DeleteByDescendantAsync(Guid descendantLedgerId, CancellationToken ct = default);
    Task DeleteSubtreeAsync(Guid rootLedgerId, CancellationToken ct = default);

    // ========================================================================
    // Hierarchy Queries (leveraging closure table)
    // ========================================================================

    /// <summary>
    /// Gets all ancestors of a ledger (bottom-up traversal).
    /// </summary>
    Task<IReadOnlyList<LedgerClosure>> GetAncestorsAsync(
        Guid descendantLedgerId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all descendants of a ledger (top-down traversal).
    /// </summary>
    Task<IReadOnlyList<LedgerClosure>> GetDescendantsAsync(
        Guid ancestorLedgerId,
        int? maxDepth = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all ancestor IDs for efficient entry posting.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAncestorIdsAsync(
        Guid descendantLedgerId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all descendant IDs for rollup queries.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetDescendantIdsAsync(
        Guid ancestorLedgerId,
        int? maxDepth = null,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if one ledger is an ancestor of another.
    /// </summary>
    Task<bool> IsAncestorOfAsync(
        Guid ancestorLedgerId,
        Guid descendantLedgerId,
        CancellationToken ct = default);
}

/// <summary>
/// Repository for LedgerEntry operations.
/// </summary>
public interface ILedgerEntryRepository
{
    // ========================================================================
    // Basic CRUD
    // ========================================================================

    Task<LedgerEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<LedgerEntry?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);

    Task<LedgerEntry?> GetByReferenceAsync(Guid tenantId, string referenceNumber)
        => GetByReferenceAsync(tenantId, referenceNumber, default);

    /// <summary>
    /// Gets an entry by its human-readable reference number (TXN-…) within a tenant.
    /// </summary>
    Task<LedgerEntry?> GetByReferenceAsync(Guid tenantId, string referenceNumber, CancellationToken ct);

    Task<IReadOnlyList<LedgerEntry>> SearchAsync(Guid tenantId, IReadOnlyList<Guid> ledgerIds)
        => SearchAsync(tenantId, ledgerIds, null, null, null, null, null, null, null, null, default);

    Task<IReadOnlyList<LedgerEntry>> SearchAsync(Guid tenantId, IReadOnlyList<Guid> ledgerIds, string? searchTerm)
        => SearchAsync(tenantId, ledgerIds, searchTerm, null, null, null, null, null, null, null, default);

    Task<IReadOnlyList<LedgerEntry>> SearchAsync(Guid tenantId, IReadOnlyList<Guid> ledgerIds, string? searchTerm, EntryType? type)
        => SearchAsync(tenantId, ledgerIds, searchTerm, type, null, null, null, null, null, null, default);

    Task<IReadOnlyList<LedgerEntry>> SearchAsync(Guid tenantId, IReadOnlyList<Guid> ledgerIds, string? searchTerm, EntryType? type, EntryCategory? category)
        => SearchAsync(tenantId, ledgerIds, searchTerm, type, category, null, null, null, null, null, default);

    Task<IReadOnlyList<LedgerEntry>> SearchAsync(Guid tenantId, IReadOnlyList<Guid> ledgerIds, string? searchTerm, EntryType? type, EntryCategory? category, EntryStatus? status)
        => SearchAsync(tenantId, ledgerIds, searchTerm, type, category, status, null, null, null, null, default);

    Task<IReadOnlyList<LedgerEntry>> SearchAsync(Guid tenantId, IReadOnlyList<Guid> ledgerIds, string? searchTerm, EntryType? type, EntryCategory? category, EntryStatus? status, decimal? minAmount)
        => SearchAsync(tenantId, ledgerIds, searchTerm, type, category, status, minAmount, null, null, null, default);

    Task<IReadOnlyList<LedgerEntry>> SearchAsync(Guid tenantId, IReadOnlyList<Guid> ledgerIds, string? searchTerm, EntryType? type, EntryCategory? category, EntryStatus? status, decimal? minAmount, decimal? maxAmount)
        => SearchAsync(tenantId, ledgerIds, searchTerm, type, category, status, minAmount, maxAmount, null, null, default);

    Task<IReadOnlyList<LedgerEntry>> SearchAsync(Guid tenantId, IReadOnlyList<Guid> ledgerIds, string? searchTerm, EntryType? type, EntryCategory? category, EntryStatus? status, decimal? minAmount, decimal? maxAmount, DateOnly? fromDate)
        => SearchAsync(tenantId, ledgerIds, searchTerm, type, category, status, minAmount, maxAmount, fromDate, null, default);

    Task<IReadOnlyList<LedgerEntry>> SearchAsync(Guid tenantId, IReadOnlyList<Guid> ledgerIds, string? searchTerm, EntryType? type, EntryCategory? category, EntryStatus? status, decimal? minAmount, decimal? maxAmount, DateOnly? fromDate, DateOnly? toDate)
        => SearchAsync(tenantId, ledgerIds, searchTerm, type, category, status, minAmount, maxAmount, fromDate, toDate, default);

    Task<IReadOnlyList<LedgerEntry>> SearchAsync(Guid tenantId, IReadOnlyList<Guid> ledgerIds, CancellationToken ct)
        => SearchAsync(tenantId, ledgerIds, null, null, null, null, null, null, null, null, ct);

    /// <summary>
    /// Searches entries across a tenant with optional ledger/type/category/status/amount/date filters.
    /// When <paramref name="ledgerIds" /> is non-empty only entries of those ledgers are returned.
    /// Tag filtering is intentionally left to the caller (list translation varies by provider).
    /// </summary>
    Task<IReadOnlyList<LedgerEntry>> SearchAsync(
        Guid tenantId,
        IReadOnlyList<Guid> ledgerIds,
        string? searchTerm,
        EntryType? type,
        EntryCategory? category,
        EntryStatus? status,
        decimal? minAmount,
        decimal? maxAmount,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken ct);

    Task AddAsync(LedgerEntry entry, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<LedgerEntry> entries, CancellationToken ct = default);
    Task UpdateAsync(LedgerEntry entry, CancellationToken ct = default);
    Task DeleteAsync(LedgerEntry entry, CancellationToken ct = default);

    // ========================================================================
    // Query by Ledger
    // ========================================================================

    /// <summary>
    /// Gets entries directly posted to a ledger (no descendants).
    /// </summary>
    Task<IReadOnlyList<LedgerEntry>> GetByLedgerAsync(
        Guid ledgerId,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets entries for a ledger including all descendants (rollup).
    /// Uses ParentLedgerIds for efficient filtering.
    /// </summary>
    Task<IReadOnlyList<LedgerEntry>> GetByLedgerIncludingDescendantsAsync(
        Guid ledgerId,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken ct = default);

    // ========================================================================
    // Sequence & Aggregations
    // ========================================================================

    Task<int> GetNextSequenceNumberAsync(Guid ledgerId, CancellationToken ct = default);

    /// <summary>
    /// Calculates balance for a ledger (direct entries only).
    /// Returns (TotalCredits, TotalDebits, EntryCount).
    /// </summary>
    Task<(decimal TotalCredits, decimal TotalDebits, int EntryCount)> GetBalanceSummaryAsync(
        Guid ledgerId,
        DateTimeOffset? asOfDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Calculates balance including all descendants (rollup).
    /// Returns (TotalCredits, TotalDebits, EntryCount).
    /// </summary>
    Task<(decimal TotalCredits, decimal TotalDebits, int EntryCount)> GetBalanceSummaryIncludingDescendantsAsync(
        Guid ledgerId,
        DateTimeOffset? asOfDate = null,
        CancellationToken ct = default);
}
