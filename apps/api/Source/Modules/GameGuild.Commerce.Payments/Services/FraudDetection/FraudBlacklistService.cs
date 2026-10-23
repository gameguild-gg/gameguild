using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     EF-backed <see cref="IFraudBlacklistService"/> (issue #409). Evaluation loads the
///     tenant's active entries of one type (a bounded set) and matches them in memory so
///     wildcard patterns (<c>*.malicious.com</c>, <c>192.168.*</c>) work identically in SQL
///     and in-memory providers.
/// </summary>
public sealed class FraudBlacklistService(
    IApplicationDbContext context,
    ILogger<FraudBlacklistService> logger) : IFraudBlacklistService
{
    /// <inheritdoc />
    public async Task<FraudBlacklistMatch> EvaluateAsync(
        Guid tenantId,
        FraudBlacklistEntryType entryType,
        string value,
        CancellationToken cancellationToken = default)
    {
        var normalized = FraudBlacklistPatterns.Normalize(value);
        if (normalized is null)
        {
            return new FraudBlacklistMatch(FraudBlacklistEvaluation.NoMatch);
        }

        var now = SystemClock.UtcNow;

        var entries = await context.Set<FraudBlacklistEntry>()
            .AsNoTracking()
            .Where(entry => entry.TenantId == tenantId
                            && entry.EntryType == entryType
                            && entry.DeletedAt == null)
            .Select(entry => new { entry.Value, entry.IsWhitelist, entry.Reason, entry.Source, entry.ExpiresAt })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        FraudBlacklistMatch? blacklistHit = null;
        foreach (var entry in entries)
        {
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value <= now)
            {
                continue;
            }

            if (!FraudBlacklistPatterns.Matches(entry.Value, normalized))
            {
                continue;
            }

            if (entry.IsWhitelist)
            {
                // Manual whitelist override wins over any blacklist entry for the same value.
                logger.LogDebug(
                    "Fraud blacklist evaluation for tenant {TenantId} type {EntryType}: value whitelisted by override",
                    tenantId,
                    entryType);
                return new FraudBlacklistMatch(FraudBlacklistEvaluation.Whitelisted);
            }

            blacklistHit ??= new FraudBlacklistMatch(
                FraudBlacklistEvaluation.Blacklisted,
                entryType,
                entry.Value,
                entry.Reason,
                entry.Source,
                entry.ExpiresAt);
        }

        return blacklistHit ?? new FraudBlacklistMatch(FraudBlacklistEvaluation.NoMatch);
    }

    /// <inheritdoc />
    public async Task<FraudBlacklistEntry> AddAsync(FraudBlacklistEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var existing = await context.Set<FraudBlacklistEntry>()
            .Where(candidate => candidate.TenantId == entry.TenantId
                                && candidate.EntryType == entry.EntryType
                                && candidate.Value == entry.Value
                                && candidate.IsWhitelist == entry.IsWhitelist
                                && candidate.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            logger.LogDebug(
                "Fraud blacklist entry for tenant {TenantId} type {EntryType} already exists; keeping existing row",
                entry.TenantId,
                entry.EntryType);
            return existing;
        }

        await context.Set<FraudBlacklistEntry>().AddAsync(entry, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Fraud blacklist entry added for tenant {TenantId} type {EntryType} (whitelist: {IsWhitelist}, source: {Source})",
            entry.TenantId,
            entry.EntryType,
            entry.IsWhitelist,
            entry.Source);

        return entry;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(Guid tenantId, Guid entryId, CancellationToken cancellationToken = default)
    {
        var entry = await context.Set<FraudBlacklistEntry>()
            .Where(candidate => candidate.Id == entryId && candidate.TenantId == tenantId && candidate.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entry is null)
        {
            return false;
        }

        entry.SoftDelete();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Fraud blacklist entry {EntryId} removed for tenant {TenantId}",
            entryId,
            tenantId);

        return true;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<FraudBlacklistEntry> Items, int TotalCount)> ListAsync(
        Guid tenantId,
        FraudBlacklistEntryType? entryType = null,
        bool includeExpired = false,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;
        var query = context.Set<FraudBlacklistEntry>()
            .AsNoTracking()
            .Where(entry => entry.TenantId == tenantId && entry.DeletedAt == null);

        if (entryType.HasValue)
        {
            var type = entryType.Value;
            query = query.Where(entry => entry.EntryType == type);
        }

        if (!includeExpired)
        {
            query = query.Where(entry => entry.ExpiresAt == null || entry.ExpiresAt > now);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(entry => entry.CreatedAt)
            .Skip((Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }
}
