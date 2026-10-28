using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Config-driven, fail-closed gateway selection (issue #413). Eligibility is the
///     conjunction of the adapter being registered, the adapter reporting
///     <c>IsEnabled</c>, and — when a <c>Payments:GatewayRouting</c> entry exists for the
///     provider — that entry being enabled and its currency/country/payment-method/amount
///     filters matching the routing context. The eligible set is then ordered by the
///     configured <see cref="GatewaySelectionStrategy" /> (priority by default; lowest
///     cost, best performance, lowest error rate, geography, round-robin and weighted
///     round-robin are opt-in). No gateway is ever selected implicitly: an empty
///     selection is surfaced through <see cref="GatewaySelectionFailedException" /> by
///     <see cref="SelectPrimary" />.
///     <para>
///         Strategies that need rolling metrics (<c>BestPerformance</c>,
///         <c>LowestErrorRate</c>) read the 24h window from
///         <see cref="IGatewayMetricsService" />; when no metrics service is available or
///         the read fails, ordering falls back to priority with a warning — the strategy
///         degrades, eligibility never does. Round-robin strategies rotate through
///         <see cref="IGatewayRotationState" /> and degrade the same way.
///     </para>
/// </summary>
public sealed class ConfigPaymentGatewaySelector : IPaymentGatewaySelector
{
    private readonly IGatewayRegistry _registry;
    private readonly GatewayRoutingOptions _options;
    private readonly ILogger<ConfigPaymentGatewaySelector> _logger;
    private readonly IGatewayMetricsService? _metrics;
    private readonly IGatewayRotationState? _rotation;

    /// <summary>
    ///     Creates the selector. The metrics and rotation collaborators are optional so
    ///     strategy-free deployments (and unit tests of pure filter behaviour) need no
    ///     fakes; every metrics/rotation-dependent strategy degrades to priority order
    ///     when they are absent.
    /// </summary>
    public ConfigPaymentGatewaySelector(
        IGatewayRegistry registry,
        IOptions<GatewayRoutingOptions> routingOptions,
        ILogger<ConfigPaymentGatewaySelector> logger,
        IGatewayMetricsService? metrics = null,
        IGatewayRotationState? rotation = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _options = (routingOptions ?? throw new ArgumentNullException(nameof(routingOptions))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = metrics;
        _rotation = rotation;
    }

    /// <inheritdoc />
    public IReadOnlyList<GatewayCandidate> Select(GatewayRouteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var (candidates, rejections) = Evaluate(context);

        if (candidates.Count == 0)
        {
            // Fail closed: record why every registered gateway was rejected; the caller
            // turns this into an explicit failure, never a silent default.
            _logger.LogWarning(
                "Gateway selection failed closed for currency {Currency}: {Rejections}",
                context.Currency,
                string.Join("; ", rejections.Select(rejection => $"{rejection.ProviderId}={rejection.Reason}")));
        }

        return candidates;
    }

    /// <inheritdoc />
    public GatewayCandidate SelectPrimary(GatewayRouteContext? context = null)
    {
        var effectiveContext = context ?? new GatewayRouteContext(string.Empty);
        var (candidates, rejections) = Evaluate(effectiveContext);

        return candidates.Count > 0
            ? candidates[0]
            : throw GatewaySelectionFailedException.Create(effectiveContext, rejections);
    }

    private (List<GatewayCandidate> Candidates, List<(string ProviderId, string Reason)> Rejections) Evaluate(
        GatewayRouteContext context)
    {
        var candidates = new List<GatewayCandidate>();
        var rejections = new List<(string ProviderId, string Reason)>();

        foreach (var gateway in _registry.Gateways)
        {
            var entry = _options.FindEntry(gateway.ProviderId);

            if (!gateway.IsEnabled)
            {
                rejections.Add((gateway.ProviderId, "adapter disabled (PaymentGateways configuration)"));
                continue;
            }

            if (entry is not null && !entry.Enabled)
            {
                rejections.Add((gateway.ProviderId, "routing entry disabled (Payments:GatewayRouting)"));
                continue;
            }

            if (!MatchesFilters(entry, context, out var filterReason))
            {
                rejections.Add((gateway.ProviderId, filterReason));
                continue;
            }

            candidates.Add(
                new GatewayCandidate(
                    gateway,
                    GatewayRoutingOptions.ResolvePriority(gateway.ProviderId, entry),
                    entry));
        }

        // OrderBy is stable, so equal priorities keep the adapter registration order.
        var orderedCandidates = candidates
            .OrderBy(candidate => candidate.Priority)
            .ToList();

        orderedCandidates = ApplyStrategy(orderedCandidates, context);

        return (orderedCandidates, rejections);
    }

    /// <summary>
    ///     Reorders the eligible candidates by the configured strategy. Every branch keeps
    ///     the eligibility verdicts untouched — strategies only reorder, never admit or
    ///     drop candidates.
    /// </summary>
    private List<GatewayCandidate> ApplyStrategy(List<GatewayCandidate> candidates, GatewayRouteContext context)
    {
        switch (_options.SelectionStrategy)
        {
            case GatewaySelectionStrategy.Priority:
                return candidates;

            case GatewaySelectionStrategy.LowestCost:
                return candidates
                    .OrderBy(candidate => ResolveEstimatedCost(candidate, context.Amount))
                    .ThenBy(candidate => candidate.Priority)
                    .ToList();

            case GatewaySelectionStrategy.BestPerformance:
            case GatewaySelectionStrategy.LowestErrorRate:
                return OrderByRollingWindowMetrics(candidates);

            case GatewaySelectionStrategy.Geography:
                return candidates
                    .OrderByDescending(candidate => ResolveCountryAffinity(candidate, context.Country))
                    .ThenBy(candidate => candidate.Priority)
                    .ToList();

            case GatewaySelectionStrategy.RoundRobin:
                return Rotate(candidates, static (rotation, list) => rotation.Rotate(list));

            case GatewaySelectionStrategy.WeightedRoundRobin:
                return Rotate(candidates, static (rotation, list) => rotation.RotateWeighted(list));

            default:
                // An unknown strategy value cannot happen after startup validation; if it
                // still does, priority order is the fail-closed disposition.
                _logger.LogWarning(
                    "Unknown gateway selection strategy {Strategy}; keeping priority order",
                    _options.SelectionStrategy);
                return candidates;
        }
    }

    /// <summary>
    ///     Estimated cost of routing one amount through the candidate: the configured fee
    ///     schedule applied to the amount, or <see cref="decimal.MaxValue" /> when the
    ///     schedule is incomplete — an unconfigured gateway is never assumed to be the
    ///     cheap one.
    /// </summary>
    private static decimal ResolveEstimatedCost(GatewayCandidate candidate, decimal? amount) =>
        candidate.Route?.EstimateFee(amount) ?? decimal.MaxValue;

    /// <summary>
    ///     Geography affinity: 2 when the entry explicitly covers the billing country, 1
    ///     when it declares no country restriction, 0 when a country is required but the
    ///     entry declares none to match (only reachable for unrestricted requests).
    /// </summary>
    private static int ResolveCountryAffinity(GatewayCandidate candidate, string? country)
    {
        if (candidate.Route is null || candidate.Route.SupportedCountries.Count == 0)
        {
            return 1;
        }

        return !string.IsNullOrWhiteSpace(country) &&
               candidate.Route.SupportedCountries.Contains(country, StringComparer.OrdinalIgnoreCase)
            ? 2
            : 0;
    }

    /// <summary>
    ///     Orders by the 24h rolling window: success rate descending
    ///     (<see cref="GatewaySelectionStrategy.BestPerformance" />) or error rate ascending
    ///     (<see cref="GatewaySelectionStrategy.LowestErrorRate" />). Gateways without
    ///     attempts in the window sort last — absence of evidence never outranks evidence —
    ///     and ties fall back to priority order. Degrades to the incoming (priority) order
    ///     when no metrics service is available or the metrics read fails.
    /// </summary>
    private List<GatewayCandidate> OrderByRollingWindowMetrics(List<GatewayCandidate> candidates)
    {
        var snapshots = LoadWindowSnapshots();
        if (snapshots.Count == 0)
        {
            return candidates;
        }

        var ascending = _options.SelectionStrategy == GatewaySelectionStrategy.LowestErrorRate;

        return candidates
            .OrderBy(candidate =>
            {
                if (!snapshots.TryGetValue(candidate.Gateway.ProviderId, out var metrics) || metrics.Attempts == 0)
                {
                    // No attempts in the window: rank after every gateway with evidence.
                    return double.MaxValue;
                }

                // A single ascending key covers both strategies: error rate ascending, or
                // negated success rate so the best success rate sorts first.
                return ascending
                    ? (double)metrics.Failed / metrics.Attempts
                    : -metrics.SuccessRate;
            })
            .ThenBy(candidate => candidate.Priority)
            .ToList();
    }

    /// <summary>
    ///     Applies the rotation through the state service, degrading to the incoming
    ///     (priority) order when no rotation state is available.
    /// </summary>
    private List<GatewayCandidate> Rotate(
        List<GatewayCandidate> candidates,
        Func<IGatewayRotationState, IReadOnlyList<GatewayCandidate>, IReadOnlyList<GatewayCandidate>> rotate)
    {
        if (_rotation is null)
        {
            _logger.LogWarning(
                "Gateway selection strategy {Strategy} requires a rotation state; none is configured, keeping priority order",
                _options.SelectionStrategy);
            return candidates;
        }

        return [.. rotate(_rotation, candidates)];
    }

    /// <summary>
    ///     Loads the 24h window snapshot per provider. Returns an empty map (never throws)
    ///     when metrics are unavailable so selection degrades to priority order.
    /// </summary>
    private Dictionary<string, GatewayWindowMetrics> LoadWindowSnapshots()
    {
        if (_metrics is null)
        {
            _logger.LogWarning(
                "Gateway selection strategy {Strategy} requires gateway metrics; none is configured, keeping priority order",
                _options.SelectionStrategy);
            return [];
        }

        try
        {
            var windows = _metrics
                .GetWindowMetricsAsync(tenantId: null, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            return windows
                .Where(window => string.Equals(window.Window, "24h", StringComparison.Ordinal))
                .GroupBy(window => window.ProviderId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The payment path must never fail because a metrics read hiccupped: fall back
            // to priority order and leave the strategy for the next selection.
            _logger.LogWarning(
                exception,
                "Reading gateway metrics for strategy {Strategy} failed; keeping priority order",
                _options.SelectionStrategy);
            return [];
        }
    }

    /// <summary>
    ///     Applies the entry's opt-in filters. An entry without a filter dimension (empty
    ///     list or null bound) does not restrict that dimension, so an unconfigured gateway
    ///     stays eligible for every request.
    /// </summary>
    private static bool MatchesFilters(GatewayRouteEntry? entry, GatewayRouteContext context, out string reason)
    {
        reason = string.Empty;

        if (entry is null)
        {
            return true;
        }

        if (entry.SupportedCurrencies.Count > 0 &&
            !entry.SupportedCurrencies.Contains(context.Currency, StringComparer.OrdinalIgnoreCase))
        {
            reason = $"currency {context.Currency} not supported";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(context.Country) &&
            entry.SupportedCountries.Count > 0 &&
            !entry.SupportedCountries.Contains(context.Country, StringComparer.OrdinalIgnoreCase))
        {
            reason = $"country {context.Country} not supported";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(context.PaymentMethodType) &&
            entry.SupportedPaymentMethods.Count > 0 &&
            !entry.SupportedPaymentMethods.Contains(context.PaymentMethodType, StringComparer.OrdinalIgnoreCase))
        {
            reason = $"payment method {context.PaymentMethodType} not supported";
            return false;
        }

        if (context.Amount.HasValue)
        {
            if (entry.MinAmount.HasValue && context.Amount.Value < entry.MinAmount.Value)
            {
                reason = $"amount {context.Amount.Value} below the gateway minimum {entry.MinAmount.Value}";
                return false;
            }

            if (entry.MaxAmount.HasValue && context.Amount.Value > entry.MaxAmount.Value)
            {
                reason = $"amount {context.Amount.Value} above the gateway maximum {entry.MaxAmount.Value}";
                return false;
            }
        }

        return true;
    }
}
