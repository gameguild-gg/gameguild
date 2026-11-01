using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.FraudDetection;

/// <summary>
///     Issue #409 remainder: the synchronous latency bound. Providers that exceed the
///     evaluation budget are deferred (recorded on the assessment, never blocking the
///     synchronous decision); the background entry point runs without the budget.
/// </summary>
public class FraudDetectionServiceBudgetTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public async Task AssessPaymentAsync_Should_Defer_A_Provider_That_Exceeds_The_Budget()
    {
        var options = Options(new FraudAssessmentOptions { SyncEvaluationBudgetMilliseconds = 20 });
        var fast = new StubProvider("FastSignal", weight: 5);
        var slow = new BlockingProvider("SlowSignal");
        var service = new FraudDetectionService(
            [fast, slow],
            options,
            NullLogger<FraudDetectionService>.Instance);

        var assessment = await service.AssessPaymentAsync(Context(), CancellationToken.None);

        assessment.EffectiveDeferredProviders.Should().Contain("SlowSignal");
        assessment.Signals.Should().ContainSingle(signal => signal.SignalType == "FastSignal");
        assessment.Score.Should().Be(5, "only the fast provider's signal contributed");
        assessment.Decision.Should().Be(FraudDecision.Allow);
    }

    [Fact]
    public async Task AssessPaymentAsync_Should_Defer_Remaining_Providers_Once_The_Budget_Is_Spent()
    {
        var options = Options(new FraudAssessmentOptions { SyncEvaluationBudgetMilliseconds = 20 });
        var slow = new BlockingProvider("SlowSignal");
        var neverCalled = new BlockingProvider("NeverCalled");
        var service = new FraudDetectionService(
            [slow, neverCalled],
            options,
            NullLogger<FraudDetectionService>.Instance);

        var assessment = await service.AssessPaymentAsync(Context(), CancellationToken.None);

        assessment.EffectiveDeferredProviders.Should().BeEquivalentTo(["SlowSignal", "NeverCalled"]);
        neverCalled.Entered.Should().BeFalse("the budget was already spent before it started");
    }

    [Fact]
    public async Task AssessPaymentAsync_Should_Propagate_The_Caller_Cancellation()
    {
        var options = Options(new FraudAssessmentOptions { SyncEvaluationBudgetMilliseconds = 5000 });
        var blocking = new BlockingProvider("Blocking");
        var service = new FraudDetectionService(
            [blocking],
            options,
            NullLogger<FraudDetectionService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var act = () => service.AssessPaymentAsync(Context(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AssessPaymentForBackgroundAsync_Should_Complete_Providers_Without_The_Budget()
    {
        var options = Options(new FraudAssessmentOptions { SyncEvaluationBudgetMilliseconds = 20 });
        var slowButFinite = new DelayedProvider("SlowSignal", weight: 45, delay: TimeSpan.FromMilliseconds(80));
        var service = new FraudDetectionService(
            [slowButFinite],
            options,
            NullLogger<FraudDetectionService>.Instance);

        var assessment = await service.AssessPaymentForBackgroundAsync(Context(), CancellationToken.None);

        assessment.EffectiveDeferredProviders.Should().BeEmpty();
        assessment.Signals.Should().ContainSingle(signal => signal.SignalType == "SlowSignal");
        assessment.Score.Should().Be(45);
    }

    [Fact]
    public async Task AssessSubscriptionChangeAsync_Should_Use_The_Change_Kind_In_The_Synchronous_Path()
    {
        var options = Options(new FraudAssessmentOptions());
        var provider = new StubProvider("AnySignal", weight: 50);
        var service = new FraudDetectionService(
            [provider],
            options,
            NullLogger<FraudDetectionService>.Instance);

        var upgrade = await service.AssessSubscriptionChangeAsync(Context(), SubscriptionFraudChangeKind.Upgrade, CancellationToken.None);
        var reactivation = await service.AssessSubscriptionChangeAsync(Context(), SubscriptionFraudChangeKind.Reactivation, CancellationToken.None);

        upgrade.Score.Should().Be(50);
        reactivation.Score.Should().Be(50);
    }

    private static IOptionsMonitor<FraudAssessmentOptions> Options(FraudAssessmentOptions options)
    {
        var monitor = new Mock<IOptionsMonitor<FraudAssessmentOptions>>();
        monitor.SetupGet(m => m.CurrentValue).Returns(options);
        return monitor.Object;
    }

    private static FraudSignalContext Context() => new(
        TenantId,
        PaymentId: Guid.NewGuid(),
        SubscriptionId: Guid.NewGuid(),
        ExternalCustomerId: "cus_test",
        PaymentMethodId: "pm_test",
        Amount: 25m,
        Currency: "USD");

    /// <summary>A provider whose evaluation blocks until cancelled — stands in for an over-budget provider.</summary>
    private sealed class BlockingProvider(string signalType) : IFraudSignalProvider
    {
        public bool Entered { get; private set; }

        public string Name => signalType;

        public async Task<IReadOnlyList<FraudSignal>> EvaluateAsync(
            FraudSignalContext signalContext,
            FraudAssessmentOptions options,
            CancellationToken cancellationToken = default)
        {
            Entered = true;
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            return [];
        }
    }

    /// <summary>A provider that completes after a finite delay longer than the test budget.</summary>
    private sealed class DelayedProvider(string signalType, int weight, TimeSpan delay) : IFraudSignalProvider
    {
        public string Name => signalType;

        public async Task<IReadOnlyList<FraudSignal>> EvaluateAsync(
            FraudSignalContext signalContext,
            FraudAssessmentOptions options,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            return [new FraudSignal(signalType, weight, "{}")];
        }
    }

    private sealed class StubProvider(string signalType, int weight) : IFraudSignalProvider
    {
        public bool Evaluated { get; private set; }

        public string Name => signalType;

        public Task<IReadOnlyList<FraudSignal>> EvaluateAsync(
            FraudSignalContext signalContext,
            FraudAssessmentOptions options,
            CancellationToken cancellationToken = default)
        {
            Evaluated = true;
            return Task.FromResult<IReadOnlyList<FraudSignal>>([new FraudSignal(signalType, weight, "{}")]);
        }
    }
}
