using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GameGuild.Commerce.Payments;

/// <summary>Outcome of one FX rate sync pass.</summary>
/// <param name="Status">Run outcome.</param>
/// <param name="ProviderName">Provider whose grid was persisted (null on total failure).</param>
/// <param name="RatesApplied">Quotations inserted or updated.</param>
/// <param name="RatesChanged">Quotations whose value changed.</param>
/// <param name="ConsecutiveFailures">Trailing failed-run count after this pass.</param>
/// <param name="EffectiveDateUtc">Effective date of the persisted grid (null on failure).</param>
/// <param name="NewestSnapshotDateUtc">Newest persisted snapshot effective date after the pass.</param>
/// <param name="ErrorMessage">Failure reason on <see cref="FxRateSyncRunStatus.Failed"/>.</param>
public sealed record FxRateSyncResult(
    FxRateSyncRunStatus Status,
    string? ProviderName,
    int RatesApplied,
    int RatesChanged,
    int ConsecutiveFailures,
    DateTime? EffectiveDateUtc,
    DateTime? NewestSnapshotDateUtc,
    string? ErrorMessage = null);

/// <summary>
///     Runs one FX rate sync pass (issue #412 spec B): fetch a rate grid from the configured
///     primary provider with retry + exponential backoff, fail over to the fallback provider
///     when the primary stays unavailable, persist the grid as <see cref="FxRateSnapshot"/> rows
///     and the run (including the raw provider response) as an <see cref="FxRateSyncRun"/>, and
///     keep the <see cref="FxRuntimeRateSource"/> current. Fail-closed throughout: nothing is
///     persisted when no provider answers, changed rates are audit-logged (rate-change audit
///     log), consecutive failures alert operators, and stale data raises a staleness alert.
/// </summary>
public interface IFxRateSyncService
{
    /// <summary>Executes one sync pass now.</summary>
    Task<FxRateSyncResult> SyncAsync(CancellationToken cancellationToken = default);
}

/// <summary>Default <see cref="IFxRateSyncService"/> implementation.</summary>
public sealed class FxRateSyncService(
    IEnumerable<IFxRateProvider> registeredProviders,
    IServiceProvider serviceProvider,
    IOptions<FxSyncOptions> options,
    FxRuntimeRateSource runtimeSource,
    IFxRateSyncAlerter alerter,
    ILogger<FxRateSyncService> logger) : IFxRateSyncService
{
    /// <inheritdoc />
    public async Task<FxRateSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var startedAt = SystemClock.UtcNow;

        FxRateSet? rateSet = null;
        string? failure = null;

        var candidates = BuildCandidateChain(registeredProviders, settings);
        foreach (var (provider, role) in candidates)
        {
            try
            {
                rateSet = await FetchWithRetryAsync(provider, settings, cancellationToken).ConfigureAwait(false);
                if (rateSet is not null)
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is FxRateProviderUnavailableException or HttpRequestException or TaskCanceledException)
            {
                failure = $"{provider.Name} ({role}): {exception.Message}";
                logger.LogWarning("FX sync fetch from {Provider} ({Role}) failed: {Message}.", provider.Name, role, exception.Message);
            }
        }

        var context = serviceProvider.GetRequiredService<IApplicationDbContext>();

        if (rateSet is null)
        {
            var result = await PersistFailedRunAsync(context, settings, startedAt, failure ?? "No FX provider configured.", cancellationToken).ConfigureAwait(false);

            if (result.ConsecutiveFailures >= settings.AlertAfterConsecutiveFailures)
            {
                await alerter
                    .RaiseSyncFailureAlertAsync(settings.Provider, result.ConsecutiveFailures, failure ?? "no provider", cancellationToken)
                    .ConfigureAwait(false);
            }

            return result;
        }

        return await PersistSuccessfulRunAsync(context, settings, startedAt, rateSet, cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<(IFxRateProvider Provider, string Role)> BuildCandidateChain(
        IEnumerable<IFxRateProvider> registeredProviders,
        FxSyncOptions settings)
    {
        var byName = registeredProviders
            .GroupBy(provider => provider.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        IFxRateProvider ResolveConfigured(string name)
        {
            return byName.TryGetValue(name, out var provider)
                ? provider
                : byName[FxSyncOptions.ProviderNone];
        }

        var chain = new List<(IFxRateProvider, string)> { (ResolveConfigured(settings.Provider), "primary") };
        if (!string.IsNullOrWhiteSpace(settings.FallbackProvider)
            && settings.FallbackProvider != FxSyncOptions.ProviderNone
            && settings.FallbackProvider != settings.Provider)
        {
            chain.Add((ResolveConfigured(settings.FallbackProvider), "fallback"));
        }

        return chain;
    }

    /// <summary>
    ///     Fetches a rate set with retry + exponential backoff (issue #412 spec B). The delay
    ///     doubles per attempt from <c>RetryBaseDelaySeconds</c>; transient transport failures
    ///     (HTTP 5xx/network) and timeouts are retried, provider-unavailable conditions (bad
    ///    config/credentials) surface immediately after the loop. Implemented inline rather than
    ///     through a resilience library: the solution pins its dependency set centrally and this
    ///     is the only outbound-retry loop in the module.
    /// </summary>
    private async Task<FxRateSet?> FetchWithRetryAsync(
        IFxRateProvider provider,
        FxSyncOptions settings,
        CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, settings.RetryMaxAttempts);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                return await provider.GetRatesAsync(asOfUtc: null, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                lastException = exception;
                logger.LogWarning(
                    "FX fetch attempt {Attempt}/{Attempts} from {Provider} failed: {Message}.",
                    attempt,
                    attempts,
                    provider.Name,
                    exception.Message);

                if (attempt < attempts)
                {
                    var delay = TimeSpan.FromSeconds(settings.RetryBaseDelaySeconds * Math.Pow(2, attempt - 1));
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        throw new FxRateProviderUnavailableException(
            provider.Name,
            $"all {attempts} fetch attempts failed: {lastException?.Message ?? "unknown transport failure"}",
            lastException);
    }

    private async Task<FxRateSyncResult> PersistSuccessfulRunAsync(
        IApplicationDbContext context,
        FxSyncOptions settings,
        DateTime startedAt,
        FxRateSet rateSet,
        CancellationToken cancellationToken)
    {
        var now = SystemClock.UtcNow;
        var rates = context.Set<FxRateSnapshot>();
        var effectiveDate = rateSet.EffectiveDateUtc.Date;
        var targetDateKeys = rateSet.Rates.Keys.Where(code => code != rateSet.BaseCurrency).ToList();

        var existing = await rates
            .Where(snapshot => !snapshot.IsOverride
                               && snapshot.EffectiveDateUtc == effectiveDate
                               && snapshot.ProviderName == rateSet.ProviderName
                               && snapshot.BaseCurrency == rateSet.BaseCurrency)
            .ToDictionaryAsync(snapshot => snapshot.QuoteCurrency, cancellationToken)
            .ConfigureAwait(false);

        var run = new FxRateSyncRun
        {
            ProviderName = rateSet.ProviderName,
            PrimaryProviderName = settings.Provider,
            StartedAtUtc = startedAt,
            CompletedAtUtc = now,
            Status = FxRateSyncRunStatus.Succeeded,
            EffectiveDateUtc = effectiveDate,
            RawResponse = rateSet.RawResponse,
            RawResponseContentType = rateSet.RawResponseContentType,
        };

        await context.Set<FxRateSyncRun>().AddAsync(run, cancellationToken).ConfigureAwait(false);

        // Rate-reasonableness guard (issue #412 spec H): the most recent previously-persisted
        // market quotations of this grid's base currency, used as the comparison baseline for
        // anomaly detection. Loaded before this pass applies anything.
        var previousRates = await LoadPreviousMarketRatesAsync(context, rateSet.BaseCurrency, effectiveDate, cancellationToken)
            .ConfigureAwait(false);

        var applied = 0;
        var changed = 0;
        var changes = new List<(string Quote, decimal OldRate, decimal NewRate)>();
        var anomalies = new List<(string Pair, decimal OldRate, decimal NewRate, decimal ChangePercent)>();

        foreach (var quoteCurrency in targetDateKeys)
        {
            var value = rateSet.Rates[quoteCurrency];

            // Quarantine quotations that moved beyond the daily-change threshold: a provider
            // glitch (or a compromised feed) must not silently reprice every market. The old
            // quotation stays authoritative; operators are alerted to review and pin manually.
            if (settings.MaxRateChangePercentPerDay > 0m
                && previousRates.TryGetValue(quoteCurrency, out var previousRate)
                && previousRate > 0m)
            {
                var changePercent = Math.Abs(value - previousRate) / previousRate * 100m;
                if (changePercent > settings.MaxRateChangePercentPerDay)
                {
                    anomalies.Add(($"{rateSet.BaseCurrency}/{quoteCurrency}", previousRate, value, changePercent));
                    continue;
                }
            }

            if (existing.TryGetValue(quoteCurrency, out var row))
            {
                if (row.Rate != value)
                {
                    changes.Add((quoteCurrency, row.Rate, value));
                    row.Rate = value;
                    changed++;
                }

                // Every returned quotation belongs to this exact publication, even if its
                // value is unchanged. Restart reconstruction must not mix old and new grids.
                row.AsOfUtc = rateSet.AsOfUtc.UtcDateTime;
                row.SyncRunId = run.Id;
                row.Touch();
                applied++;
                continue;
            }

            await rates.AddAsync(new FxRateSnapshot
            {
                BaseCurrency = rateSet.BaseCurrency,
                QuoteCurrency = quoteCurrency,
                Rate = value,
                EffectiveDateUtc = effectiveDate,
                AsOfUtc = rateSet.AsOfUtc.UtcDateTime,
                ProviderName = rateSet.ProviderName,
                Source = rateSet.Source,
                SyncRunId = run.Id,
                IsOverride = false,
            }, cancellationToken).ConfigureAwait(false);
            applied++;
            // A newly introduced quotation changes the persisted pricing state from
            // "no rate" to a value — count it as changed so markets that had no rate
            // (first sync, newly listed currency) also get their auto prices refreshed.
            changed++;
        }

        run.RatesApplied = applied;
        run.RatesChanged = changed;
        run.AnomalousRatesSkipped = anomalies.Count;
        run.AnomalyDetails = anomalies.Count > 0
            ? JsonSerializer.Serialize(anomalies.Take(200).Select(anomaly => new
            {
                pair = anomaly.Pair,
                oldRate = anomaly.OldRate,
                newRate = anomaly.NewRate,
                changePercent = Math.Round(anomaly.ChangePercent, 4),
                thresholdPercent = settings.MaxRateChangePercentPerDay,
            }))
            : null;

        // Rate-change audit log (spec B): one entry per changed quotation plus the run summary.
        foreach (var (quote, oldRate, newRate) in changes)
        {
            context.Set<AuditTrail>().Add(new AuditTrail
            {
                EntityType = nameof(FxRateSnapshot),
                EntityId = existing.TryGetValue(quote, out var row) ? row.Id : Guid.Empty,
                Action = AuditAction.Updated,
                OldValue = JsonSerializer.Serialize(new { rate = oldRate, effectiveDateUtc = effectiveDate, provider = rateSet.ProviderName }),
                NewValue = JsonSerializer.Serialize(new { rate = newRate, effectiveDateUtc = effectiveDate, provider = rateSet.ProviderName }),
                ChangedBy = Guid.Empty, // system actor: the scheduled sync worker
                ChangedAt = now,
                Metadata = JsonSerializer.Serialize(new { baseCurrency = rateSet.BaseCurrency, quoteCurrency = quote, syncRunId = run.Id }),
            });
        }

        context.Set<AuditTrail>().Add(new AuditTrail
        {
            EntityType = nameof(FxRateSyncRun),
            EntityId = run.Id,
            Action = AuditAction.Created,
            OldValue = null,
            NewValue = JsonSerializer.Serialize(new
            {
                provider = rateSet.ProviderName,
                effectiveDateUtc = effectiveDate,
                ratesApplied = applied,
                ratesChanged = changed,
                anomalousRatesSkipped = anomalies.Count,
            }),
            ChangedBy = Guid.Empty,
            ChangedAt = now,
            Metadata = JsonSerializer.Serialize(new { primaryProvider = settings.Provider, baseCurrency = rateSet.BaseCurrency }),
        });

        // Anomaly audit entries (spec H): one per quarantined quotation, so the quarantine
        // itself is reviewable even though the value was never persisted.
        foreach (var (pair, oldRate, newRate, changePercent) in anomalies.Take(50))
        {
            context.Set<AuditTrail>().Add(new AuditTrail
            {
                EntityType = nameof(FxRateSnapshot),
                EntityId = Guid.Empty,
                Action = AuditAction.Updated,
                OldValue = JsonSerializer.Serialize(new { rate = oldRate, pair }),
                NewValue = null,
                ChangedBy = Guid.Empty,
                ChangedAt = now,
                Metadata = JsonSerializer.Serialize(new
                {
                    action = "FxRateAnomalySkipped",
                    pair,
                    fetchedRate = newRate,
                    changePercent = Math.Round(changePercent, 4),
                    thresholdPercent = settings.MaxRateChangePercentPerDay,
                    syncRunId = run.Id,
                }),
            });
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (anomalies.Count > 0)
        {
            await alerter
                .RaiseRateAnomalyAlertAsync(anomalies, settings.MaxRateChangePercentPerDay, cancellationToken)
                .ConfigureAwait(false);
        }

        // Keep the conversion path current and assess staleness of the newest data.
        await runtimeSource.EnsureSeededAsync(cancellationToken).ConfigureAwait(false);
        runtimeSource.ApplyProviderSet(rateSet);

        // Auto prices follow rate updates (issue #412 spec C): when this pass actually moved
        // quotations, refresh the auto-calculated localized prices of every tenant that has
        // any. A recalculation failure must never fail the sync itself — rates are already
        // persisted and the next pass retries.
        if (changed > 0)
        {
            await RefreshLocalizedPricesAfterSyncAsync(cancellationToken).ConfigureAwait(false);
        }

        var newestSnapshotDate = await GetNewestSnapshotDateAsync(context, cancellationToken).ConfigureAwait(false)
                                 ?? effectiveDate;
        var staleDays = (now.Date - newestSnapshotDate.Date).TotalDays;
        var stale = staleDays >= settings.StalenessAlertDays;

        if (stale)
        {
            run.Status = FxRateSyncRunStatus.SucceededStale;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await alerter.RaiseStalenessAlertAsync(newestSnapshotDate, settings.StalenessAlertDays, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation(
            "FX sync succeeded via {Provider}: {Applied} rates applied, {Changed} changed, effective {Effective:yyyy-MM-dd}{Stale}.",
            rateSet.ProviderName,
            applied,
            changed,
            effectiveDate,
            stale ? " (STALE)" : string.Empty);

        return new FxRateSyncResult(
            run.Status,
            rateSet.ProviderName,
            applied,
            changed,
            ConsecutiveFailures: 0,
            EffectiveDateUtc: effectiveDate,
            NewestSnapshotDateUtc: newestSnapshotDate);
    }

    /// <summary>
    ///     Post-sync recalculation of auto-calculated localized prices (issue #412 spec C)
    ///     for every tenant holding any. Failures are logged and swallowed: the rates
    ///     themselves are already durable and recalculation re-runs on the next pass.
    /// </summary>
    private async Task RefreshLocalizedPricesAfterSyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var recalculation = scope.ServiceProvider.GetService<IPriceLocalizationRecalculationService>();
            if (recalculation is null)
            {
                return;
            }

            var tenantIds = await context.Set<PriceLocalization>()
                .AsNoTracking()
                .Where(localization => localization.IsAutoCalculated && localization.TenantId != null)
                .Select(localization => localization.TenantId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var tenantId in tenantIds)
            {
                await recalculation
                    .RecalculateAsync(context, tenantId, planId: null, triggeredByUserId: null, triggerReason: "fx-sync", cancellationToken)
                    .ConfigureAwait(false);
            }

            if (tenantIds.Count > 0)
            {
                logger.LogInformation("Refreshed auto-calculated localized prices for {Tenants} tenant(s) after the FX sync.", tenantIds.Count);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Post-sync localized-price recalculation failed; the next sync pass retries it.");
        }
    }

    private async Task<FxRateSyncResult> PersistFailedRunAsync(
        IApplicationDbContext context,
        FxSyncOptions settings,
        DateTime startedAt,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var now = SystemClock.UtcNow;
        var previousFailures = await CountTrailingFailedRunsAsync(context, cancellationToken).ConfigureAwait(false);
        var consecutiveFailures = previousFailures + 1;

        await context.Set<FxRateSyncRun>().AddAsync(new FxRateSyncRun
        {
            ProviderName = settings.Provider,
            PrimaryProviderName = settings.Provider,
            StartedAtUtc = startedAt,
            CompletedAtUtc = now,
            Status = FxRateSyncRunStatus.Failed,
            ConsecutiveFailureCount = consecutiveFailures,
            ErrorMessage = errorMessage.Length <= 4000 ? errorMessage : errorMessage[..4000],
            RawResponse = string.Empty,
            RawResponseContentType = "text/plain",
        }, cancellationToken).ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogError(
            "FX sync failed ({Failures} consecutive): {Error}. Cross-currency conversion stays on persisted rates or fails closed.",
            consecutiveFailures,
            errorMessage);

        return new FxRateSyncResult(
            FxRateSyncRunStatus.Failed,
            ProviderName: null,
            RatesApplied: 0,
            RatesChanged: 0,
            ConsecutiveFailures: consecutiveFailures,
            EffectiveDateUtc: null,
            NewestSnapshotDateUtc: await GetNewestSnapshotDateAsync(context, cancellationToken).ConfigureAwait(false),
            ErrorMessage: errorMessage);
    }

    /// <summary>
    ///     Loads the newest previously-persisted market quotation per quote currency of the
    ///     grid base, strictly before the incoming grid's effective date (any provider). Used
    ///     as the anomaly-detection baseline; overrides never participate.
    /// </summary>
    private static async Task<Dictionary<string, decimal>> LoadPreviousMarketRatesAsync(
        IApplicationDbContext context,
        string baseCurrency,
        DateTime beforeEffectiveDateUtc,
        CancellationToken cancellationToken)
    {
        var rates = context.Set<FxRateSnapshot>().AsNoTracking();

        var previousDate = await rates
            .Where(snapshot => !snapshot.IsOverride
                && snapshot.BaseCurrency == baseCurrency
                && snapshot.EffectiveDateUtc < beforeEffectiveDateUtc)
            .MaxAsync(snapshot => (DateTime?)snapshot.EffectiveDateUtc, cancellationToken)
            .ConfigureAwait(false);

        if (previousDate is null)
        {
            return [];
        }

        var rows = await rates
            .Where(snapshot => !snapshot.IsOverride
                && snapshot.BaseCurrency == baseCurrency
                && snapshot.EffectiveDateUtc == previousDate.Value)
            .Select(snapshot => new { snapshot.QuoteCurrency, snapshot.Rate, snapshot.AsOfUtc, snapshot.ProviderName })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var previous = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var row in rows.GroupBy(row => row.QuoteCurrency))
        {
            // Within one effective date the newest publication wins (same tie-break intent
            // as the runtime rate source's publication selection).
            var winner = row
                .OrderByDescending(candidate => candidate.AsOfUtc)
                .ThenBy(candidate => candidate.ProviderName, StringComparer.Ordinal)
                .First();
            previous[row.Key] = winner.Rate;
        }

        return previous;
    }

    private static async Task<int> CountTrailingFailedRunsAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var recent = await context.Set<FxRateSyncRun>()
            .AsNoTracking()
            .OrderByDescending(run => run.StartedAtUtc)
            .Take(64)
            .Select(run => run.Status)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var trailing = 0;
        foreach (var status in recent)
        {
            if (status == FxRateSyncRunStatus.Failed)
            {
                trailing++;
            }
            else
            {
                break;
            }
        }

        return trailing;
    }

    private static async Task<DateTime?> GetNewestSnapshotDateAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        return await context.Set<FxRateSnapshot>()
            .AsNoTracking()
            .MaxAsync(snapshot => (DateTime?)snapshot.EffectiveDateUtc, cancellationToken)
            .ConfigureAwait(false);
    }
}
