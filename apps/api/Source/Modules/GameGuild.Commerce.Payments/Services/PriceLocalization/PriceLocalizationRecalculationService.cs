using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>Outcome summary of one localized-price recalculation pass.</summary>
/// <param name="RunId">Progress/audit run row created for the pass.</param>
/// <param name="Status">Final run status.</param>
/// <param name="TotalItems">Candidate (plan, currency) pairs considered.</param>
/// <param name="CompletedItems">Candidates processed.</param>
/// <param name="UpdatedPrices">Auto rows whose stored value changed.</param>
/// <param name="PreservedOverrides">Manual overrides deliberately left untouched.</param>
/// <param name="SkippedForMissingRate">Candidates kept at their prior value because no FX quotation was available.</param>
public sealed record PriceRecalculationOutcome(
    Guid RunId,
    PriceRecalculationRunStatus Status,
    int TotalItems,
    int CompletedItems,
    int UpdatedPrices,
    int PreservedOverrides,
    int SkippedForMissingRate);

/// <summary>One preview row: what a recalculation pass would do, before it is applied.</summary>
/// <param name="PlanId">Plan the price belongs to.</param>
/// <param name="CurrencyCode">Localized ISO 4217 code.</param>
/// <param name="BaseMonthlyPrice">Plan monthly price in the plan currency.</param>
/// <param name="BasePlanCurrency">Plan currency code.</param>
/// <param name="CurrentPrice">Currently effective localized price, when a row applies.</param>
/// <param name="IsManualOverride">Whether the effective row is a manual override (recalculation preserves it).</param>
/// <param name="CurrentEffectiveDateUtc">Effective date of the current row.</param>
/// <param name="CurrentExpiryDateUtc">Expiry of the current row, when set.</param>
/// <param name="ProposedPrice">FX-calculated price the pass would store (absent for overrides and missing rates).</param>
/// <param name="RateUsed">Quotation that produces <paramref name="ProposedPrice"/> (target units per one plan-currency unit).</param>
/// <param name="Delta">Proposed minus current price; null when no comparison is possible.</param>
/// <param name="Note">Why no proposal is present (e.g. "manual-override-preserved", "no-rate").</param>
public sealed record LocalizedPricePreviewItem(
    Guid PlanId,
    string CurrencyCode,
    decimal BaseMonthlyPrice,
    string BasePlanCurrency,
    decimal? CurrentPrice,
    bool IsManualOverride,
    DateTime? CurrentEffectiveDateUtc,
    DateTime? CurrentExpiryDateUtc,
    decimal? ProposedPrice,
    decimal? RateUsed,
    decimal? Delta,
    string? Note);

/// <summary>
///     Bulk localized-price recalculation engine (issue #412 spec C): auto-calculate prices in
///     every enabled currency from the current FX seam, preserve manual overrides, track
///     progress on a persisted run row, and snapshot every rewritten value so the rollback
///     command can restore it. The FX sync worker reuses the same engine after each
///     successful sync pass, which is how auto prices follow rate updates.
/// </summary>
public interface IPriceLocalizationRecalculationService
{
    /// <summary>Previews the effect of a recalculation pass without writing anything.</summary>
    /// <param name="context">Database context to read from.</param>
    /// <param name="tenantId">Tenant scope.</param>
    /// <param name="planId">Optional single-plan scope; null = every active plan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<LocalizedPricePreviewItem>> PreviewAsync(
        IApplicationDbContext context,
        Guid tenantId,
        Guid? planId,
        CancellationToken cancellationToken = default);

    /// <summary>Executes a recalculation pass: creates the run row, refreshes auto prices, preserves overrides.</summary>
    /// <param name="context">Database context to write through (the caller owns the transaction).</param>
    /// <param name="tenantId">Tenant scope.</param>
    /// <param name="planId">Optional single-plan scope; null = every active plan.</param>
    /// <param name="triggeredByUserId">Acting account, or null when the FX sync worker triggered the pass.</param>
    /// <param name="triggerReason">Audit reason (e.g. "manual", "fx-sync").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PriceRecalculationOutcome> RecalculateAsync(
        IApplicationDbContext context,
        Guid tenantId,
        Guid? planId,
        Guid? triggeredByUserId,
        string triggerReason,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Snapshot of one auto row immediately before a recalculation rewrote it; the rollback
///     command replays these entries.
/// </summary>
internal sealed record PriceLocalizationBeforeEntry(
    Guid LocalizationId,
    Guid PlanId,
    string CurrencyCode,
    decimal LocalizedPrice,
    decimal? ExchangeRateUsed,
    DateTime? LastCalculatedAt);

/// <summary>Default <see cref="IPriceLocalizationRecalculationService"/>.</summary>
public sealed class PriceLocalizationRecalculationService(
    IPlanPricingResolver planPricingResolver,
    IExchangeRateProvider rates,
    ILocalizedPlanPriceResolver localizedPriceResolver) : IPriceLocalizationRecalculationService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LocalizedPricePreviewItem>> PreviewAsync(
        IApplicationDbContext context,
        Guid tenantId,
        Guid? planId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await EnumerateAsync(context, tenantId, planId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PriceRecalculationOutcome> RecalculateAsync(
        IApplicationDbContext context,
        Guid tenantId,
        Guid? planId,
        Guid? triggeredByUserId,
        string triggerReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var now = SystemClock.UtcNow;
        var planIds = await ResolvePlanScopeAsync(planId, cancellationToken).ConfigureAwait(false);
        var localizationRows = await context.Set<PriceLocalization>()
            .Where(localization => localization.TenantId == tenantId
                && (planId == null || localization.PlanId == planId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var enabledCurrencies = await LoadEnabledCurrenciesAsync(context, tenantId, cancellationToken).ConfigureAwait(false);

        var run = new PriceRecalculationRun
        {
            TenantId = tenantId,
            PlanId = planId,
            Status = PriceRecalculationRunStatus.Running,
            TriggeredByUserId = triggeredByUserId,
            TriggerReason = string.IsNullOrWhiteSpace(triggerReason) ? "manual" : triggerReason.Trim(),
            StartedAtUtc = now,
        };
        await context.Set<PriceRecalculationRun>().AddAsync(run, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var candidates = BuildCandidates(planIds, enabledCurrencies).ToList();
        run.TotalItems = candidates.Count;

        var before = new List<PriceLocalizationBeforeEntry>();
        var updated = 0;
        var preserved = 0;
        var skipped = 0;
        var completed = 0;

        foreach (var (candidatePlanId, currency) in candidates)
        {
            var planMonthly = await planPricingResolver
                .GetPlanMonthlyPriceAsync(candidatePlanId, cancellationToken)
                .ConfigureAwait(false);
            if (planMonthly is null || planMonthly.Amount <= 0m)
            {
                skipped++;
                completed++;
                continue;
            }

            var rows = localizationRows
                .Where(row => row.PlanId == candidatePlanId && row.CurrencyCode == currency)
                .OrderByDescending(row => row.EffectiveDateUtc)
                .ToList();

            if (HasActiveOrScheduledManualOverride(rows, now))
            {
                // Manual overrides are never touched by recalculation (spec C).
                preserved++;
                completed++;
                continue;
            }

            var rate = await rates.GetRateAsync(planMonthly.Currency, currency, cancellationToken).ConfigureAwait(false);
            if (rate is null || rate.Rate <= 0m)
            {
                // Fail closed: keep the prior stored value rather than guess a price.
                skipped++;
                completed++;
                continue;
            }

            var proposed = new Money(planMonthly.Amount * rate.Rate, currency);
            var target = rows.FirstOrDefault(row => row.IsAutoCalculated);
            if (target is null)
            {
                target = new PriceLocalization
                {
                    TenantId = tenantId,
                    PlanId = candidatePlanId,
                    CurrencyCode = currency,
                    IsAutoCalculated = true,
                    EffectiveDateUtc = now.Date,
                };
                await context.Set<PriceLocalization>().AddAsync(target, cancellationToken).ConfigureAwait(false);
                localizationRows.Add(target);
            }
            else
            {
                before.Add(new PriceLocalizationBeforeEntry(
                    target.Id, target.PlanId, target.CurrencyCode,
                    target.LocalizedPrice, target.ExchangeRateUsed, target.LastCalculatedAt));
            }

            if (target.LocalizedPrice != proposed.Amount)
            {
                updated++;
            }

            target.LocalizedPrice = proposed.Amount;
            target.ExchangeRateUsed = rate.Rate;
            target.LastCalculatedAt = now;
            target.Touch();
            localizedPriceResolver.Invalidate(candidatePlanId, currency);

            completed++;
            run.CompletedItems = completed;
            run.UpdatedPrices = updated;
            run.PreservedOverrides = preserved;
            run.SkippedForMissingRate = skipped;
            if (completed % 25 == 0)
            {
                // Progress checkpoints: a polling admin surface sees the run advance.
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        run.CompletedItems = completed;
        run.UpdatedPrices = updated;
        run.PreservedOverrides = preserved;
        run.SkippedForMissingRate = skipped;
        run.BeforeSnapshot = JsonSerializer.Serialize(before);
        run.Status = PriceRecalculationRunStatus.Succeeded;
        run.CompletedAtUtc = SystemClock.UtcNow;

        context.Set<AuditTrail>().Add(new AuditTrail
        {
            TenantId = tenantId,
            EntityType = nameof(PriceRecalculationRun),
            EntityId = run.Id,
            Action = AuditAction.Created,
            OldValue = null,
            NewValue = JsonSerializer.Serialize(new
            {
                run.Status,
                run.TotalItems,
                run.CompletedItems,
                run.UpdatedPrices,
                run.PreservedOverrides,
                run.SkippedForMissingRate,
                planId,
                triggerReason = run.TriggerReason,
            }),
            ChangedBy = triggeredByUserId ?? Guid.Empty, // Guid.Empty = the FX sync worker
            ChangedAt = run.CompletedAtUtc ?? SystemClock.UtcNow,
            Metadata = JsonSerializer.Serialize(new { tenantId, action = "RecalculateLocalizedPrices" }),
        });

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new PriceRecalculationOutcome(
            run.Id, run.Status, run.TotalItems, run.CompletedItems, updated, preserved, skipped);
    }

    /// <summary>
    ///     Shared enumeration for preview and execution: every (active plan, enabled currency)
    ///     pair in scope, annotated with the currently applicable row.
    /// </summary>
    private async Task<IReadOnlyList<LocalizedPricePreviewItem>> EnumerateAsync(
        IApplicationDbContext context,
        Guid tenantId,
        Guid? planId,
        CancellationToken cancellationToken)
    {
        var now = SystemClock.UtcNow;
        var planIds = await ResolvePlanScopeAsync(planId, cancellationToken).ConfigureAwait(false);
        var localizationRows = await context.Set<PriceLocalization>()
            .AsNoTracking()
            .Where(localization => localization.TenantId == tenantId
                && (planId == null || localization.PlanId == planId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var enabledCurrencies = await LoadEnabledCurrenciesAsync(context, tenantId, cancellationToken).ConfigureAwait(false);

        var items = new List<LocalizedPricePreviewItem>();
        foreach (var (candidatePlanId, currency) in BuildCandidates(planIds, enabledCurrencies))
        {
            var planMonthly = await planPricingResolver
                .GetPlanMonthlyPriceAsync(candidatePlanId, cancellationToken)
                .ConfigureAwait(false);
            if (planMonthly is null || planMonthly.Amount <= 0m)
            {
                items.Add(new LocalizedPricePreviewItem(
                    candidatePlanId, currency, 0m, string.Empty, null, false, null, null, null, null, null, "plan-unavailable"));
                continue;
            }

            var applicable = localizationRows
                .Where(row => row.PlanId == candidatePlanId && row.CurrencyCode == currency
                    && row.EffectiveDateUtc <= now && (row.ExpiryDateUtc == null || row.ExpiryDateUtc > now))
                .OrderByDescending(row => row.EffectiveDateUtc)
                .ThenByDescending(row => row.CreatedAt)
                .FirstOrDefault();

            var rate = await rates.GetRateAsync(planMonthly.Currency, currency, cancellationToken).ConfigureAwait(false);
            decimal? proposed = rate is { Rate: > 0m }
                ? new Money(planMonthly.Amount * rate.Rate, currency).Amount
                : null;

            string? note = null;
            if (applicable is { IsAutoCalculated: false })
            {
                // The override is preserved, but the preview still shows the
                // auto-calculated proposal beside it (spec C: override vs auto in
                // the admin surface) so operators can compare both values.
                note = "manual-override-preserved";
            }
            else if (proposed is null)
            {
                note = "no-rate";
            }

            items.Add(new LocalizedPricePreviewItem(
                candidatePlanId,
                currency,
                planMonthly.Amount,
                planMonthly.Currency,
                applicable?.LocalizedPrice,
                applicable is { IsAutoCalculated: false },
                applicable?.EffectiveDateUtc,
                applicable?.ExpiryDateUtc,
                proposed,
                rate?.Rate,
                proposed is not null && applicable is not null ? proposed.Value - applicable.LocalizedPrice : null,
                note));
        }

        return items;
    }

    private async Task<List<Guid>> ResolvePlanScopeAsync(Guid? planId, CancellationToken cancellationToken)
    {
        if (planId is { } single)
        {
            return [single];
        }

        var planIds = await planPricingResolver.GetActivePlanIdsAsync(cancellationToken).ConfigureAwait(false);
        return planIds.Distinct().ToList();
    }

    private static async Task<List<string>> LoadEnabledCurrenciesAsync(
        IApplicationDbContext context,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var configurations = await context.Set<CurrencyConfiguration>()
            .AsNoTracking()
            .Where(configuration => configuration.TenantId == tenantId && configuration.IsEnabled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // The base currency needs no localization: the plan is already priced in it.
        var baseCurrency = configurations
            .Where(configuration => configuration.IsBaseCurrency)
            .Select(configuration => configuration.CurrencyCode)
            .FirstOrDefault();

        return configurations
            .Select(configuration => configuration.CurrencyCode)
            .Where(code => code != baseCurrency)
            .Distinct()
            .ToList();
    }

    private static IEnumerable<(Guid PlanId, string Currency)> BuildCandidates(
        IEnumerable<Guid> planIds,
        IEnumerable<string> currencies)
    {
        foreach (var plan in planIds)
        {
            foreach (var currency in currencies)
            {
                yield return (plan, currency);
            }
        }
    }

    private static bool HasActiveOrScheduledManualOverride(IEnumerable<PriceLocalization> rows, DateTime utcNow)
    {
        // A manual override wins while it is currently applicable or scheduled for the future
        // (no expiry yet, not yet expired, or effective date still ahead). Once a dated
        // override has fully expired, auto pricing applies to that market again.
        return rows.Any(row => !row.IsAutoCalculated
            && (row.ExpiryDateUtc == null || row.ExpiryDateUtc > utcNow || row.EffectiveDateUtc > utcNow));
    }
}
