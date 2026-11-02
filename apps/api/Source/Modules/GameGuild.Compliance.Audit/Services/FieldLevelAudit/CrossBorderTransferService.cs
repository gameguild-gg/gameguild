using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameGuild.Compliance.Audit;

/// <summary>Result of validating and logging one cross-border transfer attempt.</summary>
public sealed record CrossBorderTransferEvaluation(
    CrossBorderTransferLog Log,
    CrossBorderTransferStatus Status,
    bool IsCrossBorder,
    string Reason);

public interface ICrossBorderTransferService
{
    Task<CrossBorderTransferEvaluation> ValidateAndLogAsync(
        Guid tenantId,
        Guid? userId,
        string destinationJurisdiction,
        string dataCategories,
        long recordCount,
        string purpose,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrossBorderTransferLog>> GetTransfersAsync(
        Guid? tenantId, CrossBorderTransferStatus? status, int skip, int take, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cross-border data transfer logging with jurisdiction validation (issue #186): every evaluated
/// transfer is logged with its validation outcome — allowed, requiring review, or blocked — against
/// the configured home/allowed/review jurisdictions, so the transfer history required by GDPR
/// Articles 30(1)(e) and 44-49 can be reconstructed.
/// </summary>
public sealed class CrossBorderTransferService(
    IApplicationDbContext context,
    IOptions<FieldLevelAuditOptions> options,
    TimeProvider timeProvider) : ICrossBorderTransferService
{
    public Task<CrossBorderTransferEvaluation> ValidateAndLogAsync(
        Guid tenantId,
        Guid? userId,
        string destinationJurisdiction,
        string dataCategories,
        long recordCount,
        string purpose,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationJurisdiction);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        var settings = options.Value;
        var destination = NormalizeJurisdiction(destinationJurisdiction);
        var home = NormalizeJurisdiction(settings.HomeJurisdiction);
        var allowed = settings.AllowedJurisdictions.Select(NormalizeJurisdiction).ToHashSet(StringComparer.Ordinal);
        var review = settings.ReviewJurisdictions.Select(NormalizeJurisdiction).ToHashSet(StringComparer.Ordinal);

        var (status, reason) = Validate(destination, home, allowed, review);
        var log = CrossBorderTransferLog.Record(
            tenantId, userId, destination, dataCategories, recordCount, purpose, status, reason,
            timeProvider.GetUtcNow().UtcDateTime, correlationId);
        context.Set<CrossBorderTransferLog>().Add(log);
        return SaveAndReturnAsync(log, status, destination != home, reason, cancellationToken);
    }

    internal static (CrossBorderTransferStatus Status, string Reason) Validate(
        string destination,
        string home,
        IReadOnlySet<string> allowed,
        IReadOnlySet<string> review)
    {
        if (string.Equals(destination, home, StringComparison.Ordinal))
        {
            return (CrossBorderTransferStatus.Allowed, "Domestic transfer: destination is the home jurisdiction.");
        }

        if (review.Contains(destination))
        {
            return (CrossBorderTransferStatus.RequiresReview, $"Jurisdiction '{destination}' requires a documented transfer impact assessment before personal data may flow.");
        }

        if (allowed.Contains(destination))
        {
            return (CrossBorderTransferStatus.Allowed, $"Jurisdiction '{destination}' is on the platform transfer allowlist.");
        }

        return (CrossBorderTransferStatus.Blocked, $"Jurisdiction '{destination}' is not approved for personal data transfers.");
    }

    public async Task<IReadOnlyList<CrossBorderTransferLog>> GetTransfersAsync(
        Guid? tenantId, CrossBorderTransferStatus? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = context.Set<CrossBorderTransferLog>().AsQueryable();
        if (tenantId.HasValue)
        {
            query = query.Where(transfer => transfer.TenantId == tenantId.Value);
        }
        if (status.HasValue)
        {
            query = query.Where(transfer => transfer.Status == status.Value);
        }

        return await query
            .OrderByDescending(transfer => transfer.OccurredAtUtc)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<CrossBorderTransferEvaluation> SaveAndReturnAsync(
        CrossBorderTransferLog log,
        CrossBorderTransferStatus status,
        bool isCrossBorder,
        string reason,
        CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new CrossBorderTransferEvaluation(log, status, isCrossBorder, reason);
    }

    private static string NormalizeJurisdiction(string jurisdiction) =>
        jurisdiction.Trim().ToUpperInvariant();
}
