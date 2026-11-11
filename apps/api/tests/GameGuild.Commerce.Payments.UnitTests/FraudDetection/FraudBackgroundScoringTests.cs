using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.FraudDetection;

/// <summary>
///     Issue #409 remainder + issue #1218 G3: durable deferred re-scoring. The volatile
///     in-memory DropWrite channel (jobs lost on restart, silently dropped when full) is
///     replaced by a persisted job row with a due-sweep worker: enqueued jobs survive a fresh
///     context (restart), a failed persist reports a counted best-effort miss instead of
///     throwing into the payment path, and a persistently failing job dead-letters after the
///     attempt ceiling. The escalation semantics are unchanged: the background pass can only
///     tighten a previously-allowed payment.
/// </summary>
public class FraudBackgroundScoringTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly PaymentsPersistenceTestDbContext _context = CreateContext();
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IFraudDetectionService> _fraudDetection = new();

    private static PaymentsPersistenceTestDbContext CreateContext(string? databaseName = null) => new(
        new DbContextOptionsBuilder<PaymentsPersistenceTestDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"fraud-rescore-{Guid.NewGuid():N}")
            .Options);

    private FraudBackgroundScoringJobStore CreateStore(PaymentsPersistenceTestDbContext? context = null) =>
        new(context ?? _context, NullLogger<FraudBackgroundScoringJobStore>.Instance);

    [Fact]
    public async Task Enqueued_Job_Survives_A_Restart_And_Is_Claimed_By_The_Next_Worker()
    {
        using var clock = new FrozenClock();
        var (payment, _) = NewPayment();
        var databaseName = $"fraud-rescore-restart-{Guid.NewGuid():N}";

        var enqueueContext = CreateContext(databaseName);
        (await CreateStore(enqueueContext).TryEnqueueAsync(Context(payment.Id), ["GeoIp"])).Should().BeTrue();

        // A fresh context over the same database is the restart scenario that used to lose
        // the job entirely with the in-memory channel.
        var restartedContext = CreateContext(databaseName);
        var store = CreateStore(restartedContext);
        var claimed = await store.ClaimDueAsync(10, "worker-restart");

        claimed.Should().ContainSingle().Which.Should().Match<FraudBackgroundScoringJob>(job =>
            job.PaymentId == payment.Id
            && job.Status == FraudBackgroundScoringJobStatus.Processing
            && job.AttemptCount == 1);
    }

    [Fact]
    public async Task TryEnqueue_Returns_False_And_Never_Throws_When_The_Persist_Fails()
    {
        using var clock = new FrozenClock();
        var failing = new ThrowingSaveContext();
        var store = CreateStore(failing);

        (await store.TryEnqueueAsync(Context(Guid.NewGuid()), ["GeoIp"])).Should()
            .BeFalse("a failed persist is a counted best-effort miss, never a payment-path error");
        (await store.TryEnqueueAsync(Context(Guid.NewGuid()), ["GeoIp"])).Should()
            .BeFalse("the overflow policy must keep swallowing persist failures");
    }

    [Fact]
    public async Task Worker_Escalates_An_Allowed_Payment_When_The_Background_Pass_Tightens()
    {
        using var clock = new FrozenClock();
        var (payment, _) = NewPayment();
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

        var store = CreateStore();
        await store.TryEnqueueAsync(Context(payment.Id), ["GeoIp"]);
        var worker = CreateWorker(store);

        var processed = await worker.ProcessDueAsync(CancellationToken.None);

        processed.Should().Be(1);
        payment.FraudDecision.Should().Be(FraudDecision.Review);
        payment.FraudScore.Should().Be(55);
        payment.FraudReviewStatus.Should().Be(FraudReviewStatus.PendingReview);
        payment.Status.Should().Be(PaymentStatus.Failed, "the background escalation held the unsettled payment");
        _paymentRepository.Verify(r => r.UpdateAsync(payment, It.IsAny<CancellationToken>()), Times.Once);
        (await _context.Set<FraudBackgroundScoringJob>().SingleAsync()).Status
            .Should().Be(FraudBackgroundScoringJobStatus.Completed);
    }

    [Fact]
    public async Task Worker_Leaves_The_Decision_Alone_When_The_Background_Pass_Confirms_Allow()
    {
        using var clock = new FrozenClock();
        var (payment, _) = NewPayment();
        payment.RecordFraudAssessment(FraudDecision.Allow, 5, "below review threshold", null);
        _fraudDetection
            .Setup(s => s.AssessPaymentForBackgroundAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assessment(FraudDecision.Allow, 5));

        var store = CreateStore();
        await store.TryEnqueueAsync(Context(payment.Id), ["GeoIp"]);
        await CreateWorker(store).ProcessDueAsync(CancellationToken.None);

        _paymentRepository.Verify(r => r.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never,
            "a confirming re-score never rewrites the payment");
        (await _context.Set<FraudBackgroundScoringJob>().SingleAsync()).Status
            .Should().Be(FraudBackgroundScoringJobStatus.Completed);
    }

    [Fact]
    public async Task Worker_Does_Not_Loosen_An_Already_Held_Payment()
    {
        using var clock = new FrozenClock();
        var (payment, _) = NewPayment();
        payment.RecordFraudAssessment(FraudDecision.Review, 55, "score reached the review threshold", null);
        _paymentRepository
            .Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _fraudDetection
            .Setup(s => s.AssessPaymentForBackgroundAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assessment(FraudDecision.Review, 90));

        var store = CreateStore();
        await store.TryEnqueueAsync(Context(payment.Id), ["GeoIp"]);
        await CreateWorker(store).ProcessDueAsync(CancellationToken.None);

        payment.FraudScore.Should().Be(55, "escalation is strictly a tightening operation and an existing hold stands");
        _paymentRepository.Verify(r => r.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        (await _context.Set<FraudBackgroundScoringJob>().SingleAsync()).Status
            .Should().Be(FraudBackgroundScoringJobStatus.Completed);
    }

    [Fact]
    public async Task Worker_Retries_With_Backoff_Then_DeadLetters_After_The_Attempt_Ceiling()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));
        SystemClock.SetProvider(clock);
        try
        {
            _fraudDetection
                .Setup(s => s.AssessPaymentForBackgroundAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("provider unavailable"));
            var store = CreateStore();
            await store.TryEnqueueAsync(Context(Guid.NewGuid()), ["GeoIp"]);
            var worker = CreateWorker(store);

            // Attempts 1 and 2 re-pend with an exponential NextAttemptAt gate...
            (await worker.ProcessDueAsync(CancellationToken.None)).Should().Be(1);
            var rePended = await _context.Set<FraudBackgroundScoringJob>().SingleAsync();
            rePended.Status.Should().Be(FraudBackgroundScoringJobStatus.Pending);
            rePended.NextAttemptAt.Should().NotBeNull("a failed job is retried with backoff, not dropped");

            // ...the gate holds the job until the clock passes it...
            (await worker.ProcessDueAsync(CancellationToken.None)).Should().Be(0,
                "the backoff gate must hold the job before its next attempt");
            clock.Advance(TimeSpan.FromSeconds(10));

            (await worker.ProcessDueAsync(CancellationToken.None)).Should().Be(1);
            clock.Advance(TimeSpan.FromSeconds(10));

            // ...and attempt 3 reaches the ceiling: terminal dead-letter, never re-picked.
            (await worker.ProcessDueAsync(CancellationToken.None)).Should().Be(1);
            var deadLettered = await _context.Set<FraudBackgroundScoringJob>().SingleAsync();
            deadLettered.Status.Should().Be(FraudBackgroundScoringJobStatus.DeadLettered,
                "a persistently failing job must stop retrying after the attempt ceiling (issue #1218 G3)");
            deadLettered.AttemptCount.Should().Be(FraudBackgroundScoringJobStore.MaxAttempts);
            deadLettered.NextAttemptAt.Should().BeNull();

            clock.Advance(TimeSpan.FromHours(1));
            (await store.ClaimDueAsync(10, "worker-after-dlq")).Should().BeEmpty(
                "a dead-lettered job is terminal");
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    [Fact]
    public async Task Worker_DeadLetters_A_Corrupt_Payload_Immediately()
    {
        using var clock = new FrozenClock();
        var store = CreateStore();
        await store.TryEnqueueAsync(Context(Guid.NewGuid()), ["GeoIp"]);

        var job = await _context.Set<FraudBackgroundScoringJob>().SingleAsync();
        typeof(FraudBackgroundScoringJob).GetProperty("ContextJson")!.SetValue(job, "{\"tenantId\": not-json");

        (await CreateWorker(store).ProcessDueAsync(CancellationToken.None)).Should().Be(1);
        (await _context.Set<FraudBackgroundScoringJob>().SingleAsync()).Status
            .Should().Be(FraudBackgroundScoringJobStatus.DeadLettered,
                "a corrupt payload can never succeed and must not burn the attempt budget");
        _fraudDetection.Verify(
            s => s.AssessPaymentForBackgroundAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private FraudBackgroundScoringWorker CreateWorker(FraudBackgroundScoringJobStore store) =>
        new(store, _fraudDetection.Object, _paymentRepository.Object);

    private (Payment Payment, PaymentsPersistenceTestDbContext Context) NewPayment()
    {
        var payment = Payment.Create(Guid.NewGuid(), 25m, "USD", $"key-{Guid.NewGuid():N}", subscriptionId: Guid.NewGuid());
        _context.Set<Payment>().Add(payment);
        _context.SaveChanges();
        return (payment, _context);
    }

    private static FraudSignalContext Context(Guid paymentId) => new(
        TenantId,
        PaymentId: paymentId,
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

    /// <summary>
    ///     InMemory database whose SaveChanges always fails — stands in for the persistence
    ///     outage that the old channel's DropWrite mode used to swallow silently.
    /// </summary>
    private sealed class ThrowingSaveContext : PaymentsPersistenceTestDbContext
    {
        public ThrowingSaveContext() : base(
            new DbContextOptionsBuilder<PaymentsPersistenceTestDbContext>()
                .UseInMemoryDatabase($"fraud-rescore-fail-{Guid.NewGuid():N}")
                .Options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("database unavailable");
    }

    /// <summary>Freezes the shared clock for SystemClock.UtcNow reads and restores it on dispose.</summary>
    private sealed class FrozenClock : IDisposable
    {
        public FrozenClock() => SystemClock.SetProvider(new MutableTimeProvider(DateTimeOffset.UtcNow));
        public void Dispose() => SystemClock.Reset();
    }

    private sealed class MutableTimeProvider(DateTimeOffset value) : TimeProvider
    {
        private DateTimeOffset _value = value;
        public override DateTimeOffset GetUtcNow() => _value;
        public void Advance(TimeSpan duration) => _value += duration;
    }
}
