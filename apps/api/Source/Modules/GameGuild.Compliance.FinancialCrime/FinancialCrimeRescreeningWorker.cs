using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Compliance.FinancialCrime;

/// <summary>
/// One pass of the periodic rescreening sweep (issue #1291 FC1): reads the screenings whose
/// <c>NextScreenAt</c> has come due through
/// <see cref="IFinancialCrimeControlPlane.ReadDueRescreeningsAsync"/> and re-synthesizes the
/// internal-provider ones at the next contiguous version with fresh evidence windows, so a
/// subject's financial-crime evidence never silently goes stale while activity continues.
/// External provider screenings are left to their own schedule — a real provider ingress
/// (webhook or puller) owns those.
/// </summary>
public interface IFinancialCrimeRescreeningWorker
{
    /// <summary>Processes one rescreening pass. Returns the number of screenings refreshed.</summary>
    Task<int> RunAsync(CancellationToken cancellationToken = default);
}

public sealed class FinancialCrimeRescreeningWorker(
    IFinancialCrimeControlPlane controlPlane,
    IApplicationDbContext context,
    IOptions<FinancialCrimeEconomyHookOptions> hookOptions,
    IOptions<FinancialCrimeRescreeningWorkerOptions> workerOptions,
    TimeProvider timeProvider,
    ILogger<FinancialCrimeRescreeningWorker>? logger = null) : IFinancialCrimeRescreeningWorker
{
    private readonly FinancialCrimeEconomyHookOptions _hook = (hookOptions ?? throw new ArgumentNullException(nameof(hookOptions))).Value;
    private readonly FinancialCrimeRescreeningWorkerOptions _worker =
        (workerOptions ?? throw new ArgumentNullException(nameof(workerOptions))).Value;

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var due = await controlPlane.ReadDueRescreeningsAsync(now, _worker.BatchSize, cancellationToken)
            .ConfigureAwait(false);
        var refreshed = 0;
        foreach (var screening in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!FinancialCrimeInternalScreeningProviders.IsInternal(screening.Provider))
            {
                continue;
            }

            try
            {
                await RescreenAsync(screening, now, cancellationToken).ConfigureAwait(false);
                refreshed++;
            }
            catch (FinancialCrimeConflictException exception)
            {
                // A concurrent ingestion advanced the subject's version first; the next sweep
                // retries at the new latest version.
                logger?.LogDebug(exception,
                    "Financial Crime rescreening for subject {SubjectHash} lost the version race; retrying next sweep.",
                    screening.SubjectHash);
            }
        }

        return refreshed;
    }

    private async ValueTask RescreenAsync(
        FinancialCrimeScreening screening,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var db = context as DbContext ??
            throw new InvalidOperationException(
                "Financial Crime rescreening requires the relational application DbContext.");
        var latestVersion = await db.Set<FinancialCrimeScreeningRow>().AsNoTracking()
            .Where(row => row.TenantId == screening.TenantId && row.SubjectHash == screening.SubjectHash)
            .Select(row => (long?)row.Version).MaxAsync(cancellationToken).ConfigureAwait(false) ?? 0;
        var version = latestVersion + 1;
        var providerEventId = $"rescreen:{screening.SubjectHash}:{version}";
        var canonical =
            $"{screening.Provider}|{providerEventId}|{screening.TenantId:N}|{screening.SubjectHash}|{version}|{now.UtcTicks}";
        await controlPlane.IngestScreeningAsync(screening with
        {
            Id = Guid.NewGuid(),
            ProviderEventId = providerEventId,
            Version = version,
            // The refreshed screening carries the last known outcome forward at the policy's
            // current version; a non-Approved outcome re-enters through the same case-opening
            // path (OpenCaseAndHoldAsync reuses the subject's active case when one exists).
            PolicyVersion = Math.Max(screening.PolicyVersion, _hook.PolicyVersion),
            PayloadHash = FinancialCrimeSignalHash.Sha256(canonical),
            EvidenceHash = FinancialCrimeSignalHash.Sha256($"evidence|{canonical}"),
            IssuedAt = now,
            ExpiresAt = now + _hook.ScreeningLifetime,
            NextScreenAt = now + _hook.ScreeningInterval,
            ReceivedAt = now
        }, cancellationToken).ConfigureAwait(false);
    }
}
