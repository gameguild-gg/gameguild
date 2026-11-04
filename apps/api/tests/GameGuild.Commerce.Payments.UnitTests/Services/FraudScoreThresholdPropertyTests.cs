using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GameGuild.Commerce.Payments.UnitTests.Services;

/// <summary>
///     Property-based invariants of the fraud gate's score and threshold arithmetic
///     (issue #409): aggregate scores stay inside the declared 0-100 bound for any
///     multiset of in-contract signals (each signal contributes 0-100 per
///     <see cref="FraudSignal"/>), and the decision thresholds compare with the documented
///     inclusivity — score &gt;= BlockThreshold blocks, score &gt;= ReviewThreshold reviews,
///     everything below allows. Risk bands keep their declared boundary values
///     (30/31, 60/61, 85/86).
/// </summary>
public sealed class FraudScoreThresholdPropertyTests
{
    /// <summary>A provider stub whose signals are fixed per assessment, so the score is exactly controlled.</summary>
    private sealed class FixedSignalProvider(params FraudSignal[] signals) : IFraudSignalProvider
    {
        public string Name => "FixedSignalProvider";

        public Task<IReadOnlyList<FraudSignal>> EvaluateAsync(
            FraudSignalContext context,
            FraudAssessmentOptions options,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
    }

    private static FraudAssessmentOptions OptionsWith(int reviewThreshold, int blockThreshold) => new()
    {
        IsEnabled = true,
        ReviewThreshold = reviewThreshold,
        BlockThreshold = blockThreshold,
        SyncEvaluationBudgetMilliseconds = 200
    };

    private static FraudDetectionService Service(
        FraudAssessmentOptions options,
        params FraudSignal[] signals)
    {
        var monitor = new Mock<IOptionsMonitor<FraudAssessmentOptions>>();
        monitor.SetupGet(m => m.CurrentValue).Returns(options);
        return new FraudDetectionService(
            new IFraudSignalProvider[] { new FixedSignalProvider(signals) },
            monitor.Object,
            NullLogger<FraudDetectionService>.Instance);
    }

    private static FraudSignalContext Context() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        ExternalCustomerId: null,
        PaymentMethodId: null,
        Amount: 42m,
        Currency: "USD");

    /// <summary>
    ///     For any multiset of signals with in-contract weights (0-100 each) and any valid
    ///     threshold pair (review &lt; block, both in their configured ranges): the aggregate
    ///     score is clamped into 0-100, and the decision matches the documented inclusive
    ///     comparisons — Block iff score &gt;= block, Review iff review &lt;= score &lt; block,
    ///     Allow otherwise.
    /// </summary>
    [Property(MaxTest = 400)]
    public Property ScoreStaysInDeclaredBounds_AndDecisionMatchesInclusiveThresholds(
        int[] weights,
        byte reviewThresholdLow,
        byte blockThresholdLow)
    {
        // Valid configuration per FraudAssessmentOptionsValidator: 1 <= review < block <= 100.
        var review = 1 + (reviewThresholdLow % 99);                       // 1..99
        var block = Math.Min(100, review + 1 + (blockThresholdLow % (100 - review))); // review+1..100
        var options = OptionsWith(review, block);
        var signals = weights.Select((weight, index) => new FraudSignal(
            $"Signal.{index}",
            Math.Clamp(weight, 0, 100),
            "{}")).ToArray();

        var assessment = Service(options, signals)
            .AssessPaymentAsync(Context())
            .GetAwaiter().GetResult();

        var expectedScore = Math.Min(signals.Sum(signal => signal.Weight), 100);
        var expectedDecision = expectedScore >= block ? FraudDecision.Block
            : expectedScore >= review ? FraudDecision.Review
            : FraudDecision.Allow;

        return (assessment.Score >= 0 && assessment.Score <= 100)
            .ToProperty()
            .Label($"score {assessment.Score} escaped the declared 0-100 bound")
            .And(assessment.Score.Equals(expectedScore)
                .ToProperty()
                .Label($"score {assessment.Score} does not equal min(sum of weights, 100) = {expectedScore}")
            .And(assessment.Decision.Equals(expectedDecision)
                .ToProperty()
                .Label($"score {assessment.Score} with thresholds review={review}/block={block} decided {assessment.Decision}, expected {expectedDecision}")
            .And(assessment.RiskLevel.Equals(FraudAssessment.RiskLevelForScore(assessment.Score))
                .ToProperty()
                .Label("the persisted risk band must derive from the persisted score"))));
    }

    /// <summary>
    ///     Threshold boundaries are inclusive for both bands: exactly the review threshold
    ///     reviews (review-1 allows), exactly the block threshold blocks (block-1 reviews).
    /// </summary>
    [Property(MaxTest = 400)]
    public Property Thresholds_CompareWithExactInclusivityAtBothBoundaries(byte reviewThresholdLow)
    {
        var review = 1 + (reviewThresholdLow % 50);  // 1..50 so block-1 and block stay in range
        var block = review + 25 + (reviewThresholdLow % Math.Max(1, 100 - review - 25)); // > review, <= 100
        var options = OptionsWith(review, block);

        FraudDecision DecisionOf(int score) => Service(options, new FraudSignal("Signal.Boundary", score, "{}"))
            .AssessPaymentAsync(Context())
            .GetAwaiter().GetResult()
            .Decision;

        return DecisionOf(review).Equals(FraudDecision.Review)
            .ToProperty()
            .Label($"score == ReviewThreshold ({review}) must Review (inclusive)")
            .And(DecisionOf(Math.Max(0, review - 1)).Equals(FraudDecision.Allow)
                .ToProperty()
                .Label($"score == ReviewThreshold-1 ({review - 1}) must Allow"))
            .And(DecisionOf(block).Equals(FraudDecision.Block)
                .ToProperty()
                .Label($"score == BlockThreshold ({block}) must Block (inclusive)"))
            .And(DecisionOf(block - 1).Equals(FraudDecision.Review)
                .ToProperty()
                .Label($"score == BlockThreshold-1 ({block - 1}) must Review, not Block"));
    }

    /// <summary>
    ///     Risk bands keep the declared boundaries for every score in 0..100: Low &lt;= 30,
    ///     Medium 31-60, High 61-85, Critical &gt;= 86 — the boundary scores land in the
    ///     lower band (30 Low, 60 Medium, 85 High) and the first score above each band edge
    ///     moves up (31 Medium, 61 High, 86 Critical).
    /// </summary>
    [Property(MaxTest = 101, EndSize = 101)]
    public Property RiskBands_CoverEveryScoreWithExactBoundaries(int score)
    {
        var bounded = Math.Clamp(score, 0, 100);
        var band = FraudAssessment.RiskLevelForScore(bounded);

        var expected = bounded switch
        {
            <= 30 => FraudRiskLevel.Low,
            <= 60 => FraudRiskLevel.Medium,
            <= 85 => FraudRiskLevel.High,
            _ => FraudRiskLevel.Critical
        };

        var boundaryChecks = true.ToProperty()
            .And(FraudAssessment.RiskLevelForScore(30).Equals(FraudRiskLevel.Low).ToProperty())
            .And(FraudAssessment.RiskLevelForScore(31).Equals(FraudRiskLevel.Medium).ToProperty())
            .And(FraudAssessment.RiskLevelForScore(60).Equals(FraudRiskLevel.Medium).ToProperty())
            .And(FraudAssessment.RiskLevelForScore(61).Equals(FraudRiskLevel.High).ToProperty())
            .And(FraudAssessment.RiskLevelForScore(85).Equals(FraudRiskLevel.High).ToProperty())
            .And(FraudAssessment.RiskLevelForScore(86).Equals(FraudRiskLevel.Critical).ToProperty());

        return band.Equals(expected)
            .ToProperty()
            .Label($"score {bounded} mapped to {band}, expected {expected}")
            .And(boundaryChecks);
    }
}
