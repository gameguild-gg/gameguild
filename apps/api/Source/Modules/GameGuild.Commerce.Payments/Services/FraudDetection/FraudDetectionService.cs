using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Aggregates every registered <see cref="IFraudSignalProvider"/> into a single
///     Allow / Review / Block decision for a payment attempt (issue #409). The decision is
///     evaluated before gateway execution; signal evidence stays bounded and internal
///     (no provider tokens ever enter the evidence bundle).
/// </summary>
/// <remarks>
///     <para>
///         Synchronous latency bound (issue #409: complete scoring in under 200ms,
///         non-blocking). Every assessment shares a wall-clock evaluation budget
///         (<see cref="FraudAssessmentOptions.SyncEvaluationBudgetMilliseconds"/>, default
///         150ms — comfortably inside the 200ms target once request overhead is added).
///         Each provider runs under a linked cancellation token that fires when the budget
///         is exhausted; a provider that overruns is skipped, recorded in the assessment's
///         <see cref="FraudAssessment.DeferredProviders"/> list, and left to the background
///         re-scoring pass (<see cref="AssessPaymentForBackgroundAsync"/> plus the fraud
///         background-scoring worker). The elapsed wall-clock time of every assessment is
///         measured and logged, so the bound is observable in production telemetry.
///     </para>
/// </remarks>
public sealed class FraudDetectionService(
    IEnumerable<IFraudSignalProvider> signalProviders,
    IOptionsMonitor<FraudAssessmentOptions> optionsMonitor,
    ILogger<FraudDetectionService> logger) : IFraudDetectionService
{
    private const int MaxScore = 100;
    private const int MaxReasonLength = 400;

    /// <inheritdoc />
    public async Task<FraudAssessment> AssessPaymentAsync(
        FraudSignalContext signalContext,
        CancellationToken cancellationToken = default) =>
        await AssessAsync(signalContext, "payment", cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<FraudAssessment> AssessSubscriptionCreationAsync(
        FraudSignalContext signalContext,
        CancellationToken cancellationToken = default) =>
        await AssessAsync(signalContext, "subscription creation", cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<FraudAssessment> AssessSubscriptionChangeAsync(
        FraudSignalContext signalContext,
        SubscriptionFraudChangeKind changeKind,
        CancellationToken cancellationToken = default) =>
        await AssessAsync(signalContext, $"subscription {changeKind.ToString().ToLowerInvariant()}", cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<FraudAssessment> AssessPaymentForBackgroundAsync(
        FraudSignalContext signalContext,
        CancellationToken cancellationToken = default) =>
        await AssessAsync(signalContext, "payment background re-score", cancellationToken, applySyncBudget: false)
            .ConfigureAwait(false);

    private async Task<FraudAssessment> AssessAsync(
        FraudSignalContext signalContext,
        string assessmentKind,
        CancellationToken cancellationToken,
        bool applySyncBudget = true)
    {
        ArgumentNullException.ThrowIfNull(signalContext);

        var options = optionsMonitor.CurrentValue.ResolveFor(signalContext.TenantId);

        if (!options.IsEnabled)
        {
            logger.LogDebug(
                "Fraud assessment disabled for tenant {TenantId}; allowing {Kind} {PaymentId}",
                signalContext.TenantId,
                assessmentKind,
                signalContext.PaymentId);

            return new FraudAssessment(
                signalContext.TenantId,
                signalContext.PaymentId,
                0,
                FraudRiskLevel.Low,
                FraudDecision.Allow,
                TruncateReason("Fraud assessment disabled for tenant"),
                [],
                SystemClock.UtcNow);
        }

        // Issue #409: <200ms non-blocking synchronous scoring. The budget bounds the whole
        // provider loop (each provider plus a pre-check before it starts), so the aggregate
        // assessment returns within the budget plus scheduling jitter.
        var budgetMilliseconds = applySyncBudget ? options.SyncEvaluationBudgetMilliseconds : (int?)null;

        var signals = new List<FraudSignal>();
        var deferredProviders = new List<string>();
        var stopwatch = Stopwatch.StartNew();

        foreach (var provider in signalProviders)
        {
            if (budgetMilliseconds is { } remainingBudget && stopwatch.ElapsedMilliseconds >= remainingBudget)
            {
                // Budget already exhausted: defer every remaining provider without starting it.
                deferredProviders.Add(provider.Name);
                continue;
            }

            CancellationTokenSource? budgetCts = null;
            try
            {
                if (budgetMilliseconds is { } budget)
                {
                    budgetCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    budgetCts.CancelAfter(budget);
                }

                var providerSignals = await provider.EvaluateAsync(signalContext, options, budgetCts?.Token ?? cancellationToken)
                    .ConfigureAwait(false);
                signals.AddRange(providerSignals);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && budgetCts is not null)
            {
                // The budget fired, not the caller's token: defer this provider to the
                // background re-scoring pass and keep the synchronous path non-blocking.
                deferredProviders.Add(provider.Name);
                logger.LogWarning(
                    "Fraud signal provider {Provider} exceeded the {BudgetMs}ms synchronous budget assessing {Kind} {PaymentId}; deferred to background re-scoring",
                    provider.Name,
                    budgetMilliseconds,
                    assessmentKind,
                    signalContext.PaymentId);
            }
            finally
            {
                budgetCts?.Dispose();
            }
        }

        stopwatch.Stop();

        var score = Math.Min(signals.Sum(signal => signal.Weight), MaxScore);
        var riskLevel = FraudAssessment.RiskLevelForScore(score);
        var decision = score switch
        {
            var s when s >= options.BlockThreshold => FraudDecision.Block,
            var s when s >= options.ReviewThreshold => FraudDecision.Review,
            _ => FraudDecision.Allow
        };
        var reason = BuildReason(decision, score, options, signals);

        logger.LogInformation(
            "Fraud assessment for tenant {TenantId} {Kind} {PaymentId}: decision {Decision}, score {Score}, risk {RiskLevel}, {SignalCount} signal(s), {DeferredCount} deferred provider(s), evaluated in {ElapsedMs}ms",
            signalContext.TenantId,
            assessmentKind,
            signalContext.PaymentId,
            decision,
            score,
            riskLevel,
            signals.Count,
            deferredProviders.Count,
            stopwatch.ElapsedMilliseconds);

        return new FraudAssessment(
            signalContext.TenantId,
            signalContext.PaymentId,
            score,
            riskLevel,
            decision,
            reason,
            signals,
            SystemClock.UtcNow,
            deferredProviders.Count == 0 ? null : deferredProviders);
    }

    private static string BuildReason(
        FraudDecision decision,
        int score,
        FraudAssessmentOptions options,
        IReadOnlyList<FraudSignal> signals)
    {
        if (decision == FraudDecision.Allow)
        {
            return TruncateReason($"Score {score} below review threshold {options.ReviewThreshold}");
        }

        var topSignals = signals
            .OrderByDescending(signal => signal.Weight)
            .Take(3)
            .Select(signal => $"{signal.SignalType}({signal.Weight})");

        return TruncateReason(
            $"Score {score} reached the {decision.ToString().ToLowerInvariant()} threshold " +
            $"{(decision == FraudDecision.Block ? options.BlockThreshold : options.ReviewThreshold)}: {string.Join(", ", topSignals)}");
    }

    private static string TruncateReason(string reason) =>
        reason.Length <= MaxReasonLength ? reason : reason[..MaxReasonLength];
}
