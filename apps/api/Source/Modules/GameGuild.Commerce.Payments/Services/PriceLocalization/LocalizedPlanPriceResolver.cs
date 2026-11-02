using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Default <see cref="ILocalizedPlanPriceResolver"/> (issue #412 spec C). Reads the
///     applicable <see cref="PriceLocalization"/> row for (plan, currency) — newest effective
///     date at or before now whose expiry is still open — and converts it to the requested
///     billing cycle through the plan's interval ratio. Lookups are memory-cached briefly
///     (pricing surfaces re-resolve constantly); writes invalidate through
///     <see cref="Invalidate"/>. This resolver never fabricates FX: when no row applies it
///     returns null and the caller keeps the FX-conversion path.
/// </summary>
public sealed class LocalizedPlanPriceResolver(
    IApplicationDbContext context,
    IPlanPricingResolver planPricingResolver,
    IMemoryCache cache) : ILocalizedPlanPriceResolver
{
    private const string CacheKeyPrefix = "PriceLocalization:Effective:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public async Task<Money?> ResolveLocalizedPriceAsync(
        Guid planId,
        BillingCycle billingCycle,
        Money baseCyclePrice,
        string targetCurrency,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseCyclePrice);

        var currency = (targetCurrency ?? string.Empty).Trim().ToUpperInvariant();
        if (currency.Length != 3 || baseCyclePrice.Amount <= 0m)
        {
            return null;
        }

        var row = await GetEffectiveRowAsync(planId, currency, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            return null;
        }

        // Localizations are stored at monthly granularity. For other cycles the localized
        // monthly price inherits the plan's interval discount ratio so localized markets
        // keep the same relative savings as the base market (monthly itself is the exact
        // pinned/calculated value by construction: ratio = 1).
        decimal localizedMonthly = row.LocalizedPrice;
        if (billingCycle != BillingCycle.Monthly)
        {
            var baseMonthly = await planPricingResolver
                .GetPlanMonthlyPriceAsync(planId, cancellationToken)
                .ConfigureAwait(false);
            if (baseMonthly is null || baseMonthly.Amount <= 0m)
            {
                return null; // cannot derive the ratio fail-closed rather than misprice
            }

            var ratio = baseCyclePrice.Amount / baseMonthly.Amount;
            return new Money(localizedMonthly * ratio, currency);
        }

        return new Money(localizedMonthly, currency);
    }

    /// <inheritdoc />
    public void Invalidate(Guid planId, string? currencyCode = null)
    {
        // The memory cache has no enumeration surface; bumping a per-plan revision key
        // changes every composite cache key, which is exactly the invalidation semantic.
        var revisionKey = $"{CacheKeyPrefix}rev:{planId:N}";
        var revision = cache.TryGetValue(revisionKey, out long current) ? current : 0L;
        cache.Set(revisionKey, revision + 1L, CacheDuration);
        if (!string.IsNullOrWhiteSpace(currencyCode))
        {
            cache.Remove($"{CacheKeyPrefix}{planId:N}:{currencyCode.Trim().ToUpperInvariant()}");
        }
    }

    private async Task<PriceLocalization?> GetEffectiveRowAsync(
        Guid planId,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        var revision = cache.TryGetValue($"{CacheKeyPrefix}rev:{planId:N}", out long current) ? current : 0L;
        var cacheKey = $"{CacheKeyPrefix}{planId:N}:{currencyCode}:{revision}";
        if (cache.TryGetValue(cacheKey, out PriceLocalization? cached) && cached is not null)
        {
            return cached;
        }

        var now = SystemClock.UtcNow;
        var row = await context.Set<PriceLocalization>()
            .AsNoTracking()
            .Where(localization => localization.PlanId == planId
                && localization.CurrencyCode == currencyCode
                && localization.EffectiveDateUtc <= now
                && (localization.ExpiryDateUtc == null || localization.ExpiryDateUtc > now))
            .OrderByDescending(localization => localization.EffectiveDateUtc)
            .ThenByDescending(localization => localization.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is not null)
        {
            cache.Set(cacheKey, row, CacheDuration);
        }

        return row;
    }
}
