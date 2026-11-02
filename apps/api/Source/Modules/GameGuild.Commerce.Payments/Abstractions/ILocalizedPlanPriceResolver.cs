namespace GameGuild.Commerce.Payments;

/// <summary>
///     Resolves the effective localized price of a plan in a target currency (issue #412
///     spec C). Localizations are stored per (plan, currency) at monthly granularity: a
///     manual row pins the operator's exact monthly price (psychological pricing), an auto
///     row carries the last FX-calculated monthly price. Non-monthly cycles inherit the
///     plan's interval discount ratio so localized markets keep the same relative savings.
/// </summary>
public interface ILocalizedPlanPriceResolver
{
    /// <summary>
    ///     Resolves the applicable localized price for a plan in <paramref name="targetCurrency"/>
    ///     at the current instant, or null when no effective localization row exists (callers
    ///     fall back to live FX conversion).
    /// </summary>
    /// <param name="planId">Plan whose price is being resolved.</param>
    /// <param name="billingCycle">Requested billing cycle (the ratio source for non-monthly cycles).</param>
    /// <param name="baseCyclePrice">Plan price for <paramref name="billingCycle"/> in the plan currency.</param>
    /// <param name="targetCurrency">ISO 4217 code the caller wants the price in.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Localized price, or null when no effective row applies.</returns>
    Task<Money?> ResolveLocalizedPriceAsync(
        Guid planId,
        BillingCycle billingCycle,
        Money baseCyclePrice,
        string targetCurrency,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invalidates any cached lookups for a (plan, currency) pair after a localization
    ///     write, so pricing surfaces observe manual overrides and recalculation results
    ///     immediately instead of waiting for cache expiry.
    /// </summary>
    /// <param name="planId">Plan whose localizations changed.</param>
    /// <param name="currencyCode">Affected currency, or null to invalidate every currency of the plan.</param>
    void Invalidate(Guid planId, string? currencyCode = null);
}

/// <summary>
///     Null implementation for hosts/tests that do not register localization support:
///     every plan resolves through live FX conversion exactly as before.
/// </summary>
public sealed class NullLocalizedPlanPriceResolver : ILocalizedPlanPriceResolver
{
    /// <inheritdoc />
    public Task<Money?> ResolveLocalizedPriceAsync(
        Guid planId,
        BillingCycle billingCycle,
        Money baseCyclePrice,
        string targetCurrency,
        CancellationToken cancellationToken = default)
        => Task.FromResult<Money?>(null);

    /// <inheritdoc />
    public void Invalidate(Guid planId, string? currencyCode = null)
    {
    }
}
