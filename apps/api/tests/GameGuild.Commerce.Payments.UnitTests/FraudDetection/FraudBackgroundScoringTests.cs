using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.FraudDetection;

/// <summary>
///     Issue #409 remainder: queue-based asynchronous re-scoring. The bounded queue drops
///     (never blocks) when full, and the worker escalates a previously-allowed payment
///     when the deferred background evaluation tightens the decision — leaving already-held
///     payments and unchanged outcomes untouched.
/// </summary>
public class FraudBackgroundScoringTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly PaymentsPersistenceTestDbContext _context = new(
        new DbContextOptionsBuilder<PaymentsPersistenceTestDbContext>()
            .UseInMemoryDatabase($"fraud-rescore-{Guid.NewGuid():N}")
            .Options);

    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IFraudDetectionService> _fraudDetection = new();
    private readonly FraudBackgroundScoringQueue _queue = CreateQueue();

    private static FraudBackgroundScoringQueue CreateQueue() =>
        new(
            new StubOptionsMonitor(new FraudAssessmentOptions()),
            NullLogger<FraudBackgroundScoringQueue>.Instance);

    [Fact]
    public async Task Queue_Should_Dequeue_Enqueued_Jobs_And_Report_PendingCount()
    {
        var job = new FraudBackgroundScoringJob(Guid.NewGuid(), Context(), ["GeoIp"]);

        _queue.TryEnqueue(job).Should().BeTrue();
        _queue.PendingCount.Should().Be(1);

        (await _queue.DequeueAsync(CancellationToken.None)).Should().Be(job);
        _queue.PendingCount.Should().Be(0);
    }

    [Fact]
    public async Task Worker_Should_Escalate_An_Allowed_Payment_When_The_Backend_Pass_Tightens()
    {
        var payment = NewPayment();
        payment.RecordFraudAssessment(FraudDecision.Allow, 5, "below review threshold", null);
        _paymentRepository
            .Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _paymentRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _fraudDetection
            .Setup(s => s.AssessPaymentForBackgroundAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assessment(FraudDecision.Review, 55));

        _queue.TryEnqueue(new FraudBackgroundScoringJob(payment.Id, Context(), ["GeoIp"]));
        var worker = CreateWorker();

        var processed = await worker.ProcessOneAsync(CancellationToken.None);

        processed.Should().BeTrue();
        payment.FraudDecision.Should().Be(FraudDecision.Review);
        payment.FraudScore.Should().Be(55);
        payment.FraudReviewStatus.Should().Be(FraudReviewStatus.PendingReview);
        payment.Status.Should().Be(PaymentStatus.Failed, "the background escalation held the unsettled payment");
        _paymentRepository.Verify(r => r.UpdateAsync(payment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Worker_Should_Leave_The_Decision_Alone_When_The_Background_Pass_Confirms_Allow()
    {
        var payment = NewPayment();
        payment.RecordFraudAssessment(FraudDecision.Allow, 5, "below review threshold", null);
        _fraudDetection
            .Setup(s => s.AssessPaymentForBackgroundAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assessment(FraudDecision.Allow, 5));

        _queue.TryEnqueue(new FraudBackgroundScoringJob(payment.Id, Context(), ["GeoIp"]));
        var worker = CreateWorker();

        await worker.ProcessOneAsync(CancellationToken.None);

        _paymentRepository.Verify(r => r.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never,
            "a confirming re-score never rewrites the payment");
    }

    [Fact]
    public async Task Worker_Should_Not_Loosen_An_Already_Held_Payment()
    {
        var payment = NewPayment();
        payment.RecordFraudAssessment(FraudDecision.Review, 55, "score reached the review threshold", null);
        _paymentRepository
            .Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _fraudDetection
            .Setup(s => s.AssessPaymentForBackgroundAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assessment(FraudDecision.Review, 90));

        _queue.TryEnqueue(new FraudBackgroundScoringJob(payment.Id, Context(), ["GeoIp"]));
        var worker = CreateWorker();

        await worker.ProcessOneAsync(CancellationToken.None);

        payment.FraudScore.Should().Be(55, "escalation is strictly a tightening operation and an existing hold stands");
        _paymentRepository.Verify(r => r.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Worker_Should_Isolate_Per_Job_Failures()
    {
        _fraudDetection
            .Setup(s => s.AssessPaymentForBackgroundAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        _queue.TryEnqueue(new FraudBackgroundScoringJob(Guid.NewGuid(), Context(), ["GeoIp"]));
        var worker = CreateWorker();

        var processed = await worker.ProcessOneAsync(CancellationToken.None);

        processed.Should().BeTrue("the job was consumed even though it failed");
    }

    [Fact]
    public async Task Worker_Should_Return_False_When_The_Queue_Is_Empty()
    {
        var worker = CreateWorker();

        var processed = await worker.ProcessOneAsync(CancellationToken.None);

        processed.Should().BeFalse();
    }

    private FraudBackgroundScoringWorker CreateWorker() =>
        new(_queue, _fraudDetection.Object, _paymentRepository.Object);

    private Payment NewPayment()
    {
        var payment = Payment.Create(Guid.NewGuid(), 25m, "USD", $"key-{Guid.NewGuid():N}", subscriptionId: Guid.NewGuid());
        _context.Set<Payment>().Add(payment);
        _context.SaveChanges();
        return payment;
    }

    private static FraudSignalContext Context() => new(
        Guid.NewGuid(),
        PaymentId: Guid.NewGuid(),
        SubscriptionId: Guid.NewGuid(),
        ExternalCustomerId: "cus_test",
        PaymentMethodId: "pm_test",
        Amount: 25m,
        Currency: "USD");

    private static FraudAssessment Assessment(FraudDecision decision, int score) =>
        new(
            TenantId,
            Guid.Empty,
            score,
            FraudAssessment.RiskLevelForScore(score),
            decision,
            $"unit-test {decision}",
            [],
            SystemClock.UtcNow);

    /// <summary>Minimal IOptionsMonitor stand-in for unit tests.</summary>
    private sealed class StubOptionsMonitor(FraudAssessmentOptions value) : IOptionsMonitor<FraudAssessmentOptions>
    {
        public FraudAssessmentOptions CurrentValue => value;

        public FraudAssessmentOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<FraudAssessmentOptions, string?> listener) => null;
    }
}
