using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>Admin view of one (plan, currency) localization: override vs auto-calculated.</summary>
/// <param name="PlanId">Plan the row belongs to.</param>
/// <param name="CurrencyCode">Localized ISO 4217 code.</param>
/// <param name="EffectivePrice">Currently effective localized price, when a row applies.</param>
/// <param name="IsManualOverride">Whether the effective row is a manual override.</param>
/// <param name="EffectiveDateUtc">Effective date of the row.</param>
/// <param name="ExpiryDateUtc">Expiry of the row, when set.</param>
/// <param name="ExchangeRateUsed">Rate that produced the last auto calculation.</param>
/// <param name="LastCalculatedAt">Moment of the last auto calculation.</param>
/// <param name="AutoPriceToday">What the FX-calculated auto price would be right now (reference for override comparison).</param>
/// <param name="AutoPriceRate">Quotation behind <paramref name="AutoPriceToday"/>, when available.</param>
/// <param name="Note">Why a reference price is absent (e.g. "no-rate").</param>
public sealed record PlanLocalizedPriceDto(
    Guid PlanId,
    string CurrencyCode,
    decimal? EffectivePrice,
    bool IsManualOverride,
    DateTime? EffectiveDateUtc,
    DateTime? ExpiryDateUtc,
    decimal? ExchangeRateUsed,
    DateTime? LastCalculatedAt,
    decimal? AutoPriceToday,
    decimal? AutoPriceRate,
    string? Note = null);

/// <summary>
///     Query (issue #412 spec C): the admin pricing view for one plan (or every plan) — the
///     effective localization per enabled currency annotated with the live FX-calculated
///     reference price, so operators see override vs auto-calculated side by side.
/// </summary>
[AuthorizeRequest("", RequireTenantAdmin = true)]
public sealed record GetPlanLocalizedPricesQuery(Guid? PlanId = null) : IQuery<IReadOnlyList<PlanLocalizedPriceDto>>;

/// <summary>Handler for <see cref="GetPlanLocalizedPricesQuery"/>.</summary>
public sealed class GetPlanLocalizedPricesQueryHandler(
    IApplicationDbContext context,
    IPriceLocalizationRecalculationService recalculationService,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<GetPlanLocalizedPricesQuery, IReadOnlyList<PlanLocalizedPriceDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanLocalizedPriceDto>> Handle(GetPlanLocalizedPricesQuery request, CancellationToken cancellationToken)
    {
        var (tenantId, _) = CurrencyConfigurationCommandHelpers.ResolveActor(actorContextAccessor);
        var now = SystemClock.UtcNow;

        var preview = await recalculationService
            .PreviewAsync(context, tenantId, request.PlanId, cancellationToken)
            .ConfigureAwait(false);
        var previewByPair = preview.ToDictionary(
            item => (item.PlanId, item.CurrencyCode),
            item => item);

        // Effective row per (plan, currency): newest effective date at or before now whose
        // expiry is still open — the same applicability rule the pricing engine applies.
        var effectiveRows = await context.Set<PriceLocalization>()
            .AsNoTracking()
            .Where(localization => localization.TenantId == tenantId
                && (request.PlanId == null || localization.PlanId == request.PlanId)
                && localization.EffectiveDateUtc <= now
                && (localization.ExpiryDateUtc == null || localization.ExpiryDateUtc > now))
            .OrderByDescending(localization => localization.EffectiveDateUtc)
            .ThenByDescending(localization => localization.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var rowsByPair = new Dictionary<(Guid PlanId, string Currency), PriceLocalization>();
        foreach (var row in effectiveRows)
        {
            rowsByPair.TryAdd((row.PlanId, row.CurrencyCode), row);
        }

        var dtos = new List<PlanLocalizedPriceDto>();
        foreach (var ((planId, currencyCode), row) in rowsByPair)
        {
            var reference = previewByPair.TryGetValue((planId, currencyCode), out var item) ? item : null;
            dtos.Add(new PlanLocalizedPriceDto(
                planId,
                currencyCode,
                row.LocalizedPrice,
                !row.IsAutoCalculated,
                row.EffectiveDateUtc,
                row.ExpiryDateUtc,
                row.ExchangeRateUsed,
                row.LastCalculatedAt,
                reference?.ProposedPrice,
                reference?.RateUsed,
                reference?.Note));
        }

        return dtos;
    }
}

/// <summary>
///     Query (issue #412 spec C): preview what a recalculation pass would change — proposed
///     auto prices against currently effective values — without writing anything.
/// </summary>
[AuthorizeRequest("", RequireTenantAdmin = true)]
public sealed record PreviewLocalizedPricesRecalculationQuery(Guid? PlanId = null) : IQuery<IReadOnlyList<LocalizedPricePreviewItem>>;

/// <summary>Handler for <see cref="PreviewLocalizedPricesRecalculationQuery"/>.</summary>
public sealed class PreviewLocalizedPricesRecalculationQueryHandler(
    IApplicationDbContext context,
    IPriceLocalizationRecalculationService recalculationService,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<PreviewLocalizedPricesRecalculationQuery, IReadOnlyList<LocalizedPricePreviewItem>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LocalizedPricePreviewItem>> Handle(PreviewLocalizedPricesRecalculationQuery request, CancellationToken cancellationToken)
    {
        var (tenantId, _) = CurrencyConfigurationCommandHelpers.ResolveActor(actorContextAccessor);
        return await recalculationService
            .PreviewAsync(context, tenantId, request.PlanId, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>One recalculation run as exposed to the admin surface (progress + outcome).</summary>
/// <param name="Id">Run identifier.</param>
/// <param name="PlanId">Plan scope; null = every plan.</param>
/// <param name="Status">Run status.</param>
/// <param name="TotalItems">Candidate count.</param>
/// <param name="CompletedItems">Processed count (progress).</param>
/// <param name="UpdatedPrices">Auto rows changed.</param>
/// <param name="PreservedOverrides">Manual overrides preserved.</param>
/// <param name="SkippedForMissingRate">Candidates skipped for lack of a quotation.</param>
/// <param name="TriggeredByUserId">Acting account; null = FX sync worker.</param>
/// <param name="TriggerReason">Why the run happened.</param>
/// <param name="StartedAtUtc">Start instant.</param>
/// <param name="CompletedAtUtc">Completion instant; null while running.</param>
/// <param name="RolledBackAtUtc">Rollback instant, when rolled back.</param>
public sealed record PriceRecalculationRunDto(
    Guid Id,
    Guid? PlanId,
    PriceRecalculationRunStatus Status,
    int TotalItems,
    int CompletedItems,
    int UpdatedPrices,
    int PreservedOverrides,
    int SkippedForMissingRate,
    Guid? TriggeredByUserId,
    string TriggerReason,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? RolledBackAtUtc);

/// <summary>
///     Query (issue #412 spec C): recalculation runs with their progress counters — newest
///     first, capped at <paramref name="Limit"/> (1..100).
/// </summary>
[AuthorizeRequest("", RequireTenantAdmin = true)]
public sealed record GetPriceRecalculationRunsQuery(int Limit = 20) : IQuery<IReadOnlyList<PriceRecalculationRunDto>>;

/// <summary>Handler for <see cref="GetPriceRecalculationRunsQuery"/>.</summary>
public sealed class GetPriceRecalculationRunsQueryHandler(
    IApplicationDbContext context,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<GetPriceRecalculationRunsQuery, IReadOnlyList<PriceRecalculationRunDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PriceRecalculationRunDto>> Handle(GetPriceRecalculationRunsQuery request, CancellationToken cancellationToken)
    {
        var (tenantId, _) = CurrencyConfigurationCommandHelpers.ResolveActor(actorContextAccessor);
        var limit = Math.Clamp(request.Limit, 1, 100);

        return await context.Set<PriceRecalculationRun>()
            .AsNoTracking()
            .Where(run => run.TenantId == tenantId)
            .OrderByDescending(run => run.StartedAtUtc)
            .Take(limit)
            .Select(run => new PriceRecalculationRunDto(
                run.Id,
                run.PlanId,
                run.Status,
                run.TotalItems,
                run.CompletedItems,
                run.UpdatedPrices,
                run.PreservedOverrides,
                run.SkippedForMissingRate,
                run.TriggeredByUserId,
                run.TriggerReason,
                run.StartedAtUtc,
                run.CompletedAtUtc,
                run.RolledBackAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
