using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Selection strategy applied to the eligible gateway candidates (issue #413 routing
///     expansion). <see cref="Priority" /> remains the fail-closed default; every other
///     strategy reorders the same eligible set without weakening the eligibility
///     conjunction (adapter registered + adapter enabled + routing entry enabled +
///     currency/country/payment-method/amount filters).
/// </summary>
public enum GatewaySelectionStrategy
{
    /// <summary>Ascending <c>Priority</c>; stable ties keep registration order (default).</summary>
    Priority = 0,

    /// <summary>
    ///     Lowest estimated transaction cost first, from the per-gateway fee schedule
    ///     (<c>FeeFixedAmount</c> + <c>FeePercentBasisPoints</c> of the amount). Gateways
    ///     without a fee schedule sort last (unknown cost is never assumed to be cheap),
    ///     then by priority.
    /// </summary>
    LowestCost = 1,

    /// <summary>
    ///     Highest 24h success rate first (from <see cref="IGatewayMetricsService" />).
    ///     Gateways without attempts in the window sort last — no evidence is never
    ///     treated as perfect — then by priority.
    /// </summary>
    BestPerformance = 2,

    /// <summary>
    ///     Lowest 24h error rate (<c>Failed / Attempts</c>) first. Gateways without
    ///     attempts in the window sort last, then by priority.
    /// </summary>
    LowestErrorRate = 3,

    /// <summary>
    ///     Gateways whose <c>SupportedCountries</c> explicitly covers the request's
    ///     billing country sort first, then unrestricted gateways, then by priority.
    ///     Requires the caller to supply the country through the routing context.
    /// </summary>
    Geography = 4,

    /// <summary>
    ///     Even rotation over the eligible set through a process-wide counter, so
    ///     consecutive charges spread across gateways regardless of priority.
    /// </summary>
    RoundRobin = 5,

    /// <summary>
    ///     Smooth weighted round-robin (nginx-style accumulators) using each entry's
    ///     <c>Weight</c>; unconfigured entries default to weight 1.
    /// </summary>
    WeightedRoundRobin = 6
}

/// <summary>
///     Configuration-driven gateway routing (issue #413), bound from <c>Payments:GatewayRouting</c>.
///     Each entry governs one registered gateway: whether it participates in routing, its priority
///     (lower value wins), its routing weight and fee schedule, and its eligibility filters. An
///     empty list keeps the fail-closed default disposition: every gateway registered in DI that
///     reports <c>IsEnabled</c> participates, ordered by the well-known provider priorities
///     (stripe first, then paypal, then everything else).
/// </summary>
public sealed class GatewayRoutingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Payments:GatewayRouting";

    /// <summary>
    ///     Ordering strategy applied to the eligible candidates. Defaults to
    ///     <see cref="GatewaySelectionStrategy.Priority" /> (the original fail-closed
    ///     behaviour); an out-of-range configured value is rejected by startup validation.
    /// </summary>
    public GatewaySelectionStrategy SelectionStrategy { get; set; } = GatewaySelectionStrategy.Priority;

    /// <summary>
    ///     Per-gateway routing entries. Absent entries fall back to the adapter's own
    ///     <c>IsEnabled</c> disposition with the well-known default priority.
    /// </summary>
    public List<GatewayRouteEntry> Gateways { get; set; } = [];

    /// <summary>
    ///     Resolves the configured route entry for a provider, if one exists. Provider
    ///     identifiers are compared case-insensitively.
    /// </summary>
    public GatewayRouteEntry? FindEntry(string providerId) =>
        Gateways.FirstOrDefault(entry =>
            string.Equals(entry.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     Resolves the effective priority for a provider: the configured value, or the
    ///     well-known default (stripe 100, paypal 200, authorizenet 300, braintree 400,
    ///     adyen 500, unknown adapters 1000).
    /// </summary>
    public static int ResolvePriority(string providerId, GatewayRouteEntry? entry) =>
        entry?.Priority ?? providerId.ToLowerInvariant() switch
        {
            GatewayProviderIds.Stripe => 100,
            GatewayProviderIds.PayPal => 200,
            GatewayProviderIds.AuthorizeNet => 300,
            GatewayProviderIds.Braintree => 400,
            GatewayProviderIds.Adyen => 500,
            _ => 1000
        };
}

/// <summary>
///     Well-known payment gateway provider identifiers used inside the Payments module
///     (mirrors the Commerce.Billing <c>PaymentProviders</c> constants without a module dependency).
/// </summary>
public static class GatewayProviderIds
{
    /// <summary>Stripe provider identifier.</summary>
    public const string Stripe = "stripe";

    /// <summary>PayPal provider identifier.</summary>
    public const string PayPal = "paypal";

    /// <summary>Authorize.Net provider identifier.</summary>
    public const string AuthorizeNet = "authorizenet";

    /// <summary>Braintree provider identifier.</summary>
    public const string Braintree = "braintree";

    /// <summary>Adyen provider identifier.</summary>
    public const string Adyen = "adyen";
}

/// <summary>
///     One gateway's routing configuration. Filter lists are opt-in restrictions:
///     an empty list means "no restriction" (every currency, country or payment
///     method is eligible); a non-empty list must contain the request's value.
/// </summary>
public sealed class GatewayRouteEntry
{
    /// <summary>Provider identifier of the registered gateway (stripe, paypal, ...).</summary>
    [Required]
    [MaxLength(50)]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    ///     Whether the gateway participates in routing. When an entry exists this flag and the
    ///     adapter's own <c>IsEnabled</c> must both be true (fail-closed conjunction).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Selection priority; lower values are tried first. Ties fall back to the
    ///     registration order of the adapters.
    /// </summary>
    [Range(1, 10000)]
    public int Priority { get; set; } = 100;

    /// <summary>
    ///     Relative routing weight for the <see cref="GatewaySelectionStrategy.WeightedRoundRobin" />
    ///     strategy (smooth weighted round-robin). Must be at least 1; the value is ignored by
    ///     every other strategy.
    /// </summary>
    [Range(1, 1000)]
    public int Weight { get; set; } = 1;

    /// <summary>
    ///     Per-transaction fixed fee (major currency units, e.g. 0.30) used by the
    ///     <see cref="GatewaySelectionStrategy.LowestCost" /> strategy and the cost
    ///     analytics; null means no fixed component is configured.
    /// </summary>
    [Range(0, 1000)]
    public decimal? FeeFixedAmount { get; set; }

    /// <summary>
    ///     Percentage fee in basis points (100 bp = 1%) used by the
    ///     <see cref="GatewaySelectionStrategy.LowestCost" /> strategy and the cost
    ///     analytics; null means no percentage component is configured.
    /// </summary>
    [Range(0, 3000)]
    public int? FeePercentBasisPoints { get; set; }

    /// <summary>
    ///     Whether a complete fee schedule is configured: both the fixed and the percentage
    ///     component are present. Cost-based ordering treats gateways without a complete
    ///     schedule as unknown cost (sorted last), never as free.
    /// </summary>
    public bool HasFeeSchedule => FeeFixedAmount.HasValue && FeePercentBasisPoints.HasValue;

    /// <summary>
    ///     Estimates the transaction fee for an amount: fixed component + percentage
    ///     component of the amount. Returns null when the fee schedule is incomplete.
    /// </summary>
    public decimal? EstimateFee(decimal? amount) => !HasFeeSchedule
        ? null
        : FeeFixedAmount!.Value + (amount ?? 0m) * FeePercentBasisPoints!.Value / 10000m;

    /// <summary>ISO 4217 currency codes this gateway accepts; empty means all.</summary>
    public List<string> SupportedCurrencies { get; set; } = [];

    /// <summary>ISO 3166-1 alpha-2 billing countries this gateway accepts; empty means all.</summary>
    public List<string> SupportedCountries { get; set; } = [];

    /// <summary>Payment method types this gateway accepts (card, paypal, ...); empty means all.</summary>
    public List<string> SupportedPaymentMethods { get; set; } = [];

    /// <summary>Inclusive minimum amount (major currency units) this gateway accepts; null means no bound.</summary>
    public decimal? MinAmount { get; set; }

    /// <summary>Inclusive maximum amount (major currency units) this gateway accepts; null means no bound.</summary>
    public decimal? MaxAmount { get; set; }
}

/// <summary>
///     Startup validation for <see cref="GatewayRoutingOptions" />: rejects duplicate provider
///     entries, unknown empty provider identifiers, inverted amount bounds, out-of-range
///     strategy values, and invalid weights or fee schedules.
/// </summary>
public sealed class GatewayRoutingOptionsValidator : IValidateOptions<GatewayRoutingOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, GatewayRoutingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (!Enum.IsDefined(options.SelectionStrategy))
        {
            failures.Add(
                $"SelectionStrategy must be one of {string.Join(", ", Enum.GetNames<GatewaySelectionStrategy>())}.");
        }

        var seenProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in options.Gateways)
        {
            if (string.IsNullOrWhiteSpace(entry.ProviderId))
            {
                failures.Add("Gateway routing entries must declare a provider id.");
                continue;
            }

            if (!seenProviders.Add(entry.ProviderId))
            {
                failures.Add($"Duplicate gateway routing entry for provider '{entry.ProviderId}'.");
            }

            if (entry.Priority is < 1 or > 10000)
            {
                failures.Add($"Provider '{entry.ProviderId}': Priority must be between 1 and 10000.");
            }

            if (entry.Weight is < 1 or > 1000)
            {
                failures.Add($"Provider '{entry.ProviderId}': Weight must be between 1 and 1000.");
            }

            if (entry.MinAmount.HasValue && entry.MaxAmount.HasValue && entry.MinAmount.Value > entry.MaxAmount.Value)
            {
                failures.Add($"Provider '{entry.ProviderId}': MinAmount cannot exceed MaxAmount.");
            }

            if (entry.FeeFixedAmount.HasValue && entry.FeeFixedAmount.Value < 0)
            {
                failures.Add($"Provider '{entry.ProviderId}': FeeFixedAmount cannot be negative.");
            }

            if (entry.FeePercentBasisPoints is < 0 or > 3000)
            {
                failures.Add($"Provider '{entry.ProviderId}': FeePercentBasisPoints must be between 0 and 3000.");
            }

            // A half-configured fee schedule would silently order by its partial value; cost
            // strategies require both components or none.
            if (entry.FeeFixedAmount.HasValue != entry.FeePercentBasisPoints.HasValue)
            {
                failures.Add(
                    $"Provider '{entry.ProviderId}': FeeFixedAmount and FeePercentBasisPoints must be configured together.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
