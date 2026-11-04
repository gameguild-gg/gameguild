using FluentAssertions;
using GameGuild.Commerce.Payments;
using GameGuild.Identity.Context.Actors;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Commands;

/// <summary>
///     Refund money-integrity guarantees (issue #1202):
///     N2 — a concurrent refund leg (gateway execution vs webhook completion) cannot corrupt
///          the cumulative RefundedAmount: the Version optimistic-concurrency token surfaces
///          the conflict and the completion core reloads the fresh aggregate and re-applies
///          its own leg against it;
///     N3 — webhook completion verifies the gateway-reported amount against the local refund
///          row before finalizing and fails closed on a mismatch;
///     N6 — the pending-payment aging sweep exits outcome-unknown stuck-Pending payments as
///          failed-with-reason, non-retriable.
/// </summary>
public class RefundConcurrencyAndVerificationTests
{
    private readonly Mock<IRevenueAuditService> _revenueAudit = new();
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly Mock<INotificationService> _notifications = new();

    public RefundConcurrencyAndVerificationTests()
    {
        _actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(AuthenticatedActor());
        _revenueAudit
            .Setup(audit => audit.RecordRevenueEventAsync(
                It.IsAny<RevenueEventType>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<RevenueSource>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevenueEvent { Id = Guid.NewGuid() });
        _notifications
            .Setup(service => service.SendAsync(
                It.IsAny<Guid?>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationChannel>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Notification.Create(
                Guid.NewGuid(),
                NotificationType.RefundCompleted,
                NotificationChannel.Email,
                "Refund processed",
                "Your refund was processed.",
                null)));
    }

    // ───────────────────────── N2: concurrent refund legs ─────────────────────────

    [Fact]
    public async Task CompleteFromGatewayAsync_ConcurrentRefundLeg_ReloadsAndReappliesAgainstTheFreshCumulative()
    {
        var storeName = $"refunds-{Guid.NewGuid():N}";
        var payment = SucceededPayment(100m);
        await using (var seed = NewDatabase(storeName))
        {
            seed.Add(payment);
            await seed.SaveChangesAsync();
        }

        // The service's scoped context loads its copy FIRST (stale: RefundedAmount 0, Version 1).
        await using var serviceDb = NewDatabase(storeName);
        var stale = await serviceDb.Set<Payment>().SingleAsync(p => p.Id == payment.Id);

        // A concurrent refund leg completes on a different context and persists its 50 leg.
        await using (var otherDb = NewDatabase(storeName))
        {
            var concurrent = await otherDb.Set<Payment>().SingleAsync(p => p.Id == payment.Id);
            concurrent.ProcessRefund(50m, "re_concurrent_leg", "duplicate");
            await otherDb.SaveChangesAsync();
        }

        var refund = Refund.Create(
            payment.TenantId,
            payment.Id,
            30m,
            "USD",
            "requested_by_customer",
            null,
            Guid.NewGuid(),
            requiresApproval: false,
            $"idem-concurrent-{Guid.NewGuid():N}",
            kind: RefundKind.Partial);
        serviceDb.Add(refund);
        await serviceDb.SaveChangesAsync();
        var service = CreateService(serviceDb);

        var outcome = await service.CompleteFromGatewayAsync(refund, "re_webhook_leg", "stripe");

        outcome.Success.Should().BeTrue(
            "the concurrent leg must surface as a reload-and-retry, not a corrupted or lost completion");
        stale.RefundedAmount.Should().Be(80m,
            "the webhook leg (30) is re-applied against the concurrently persisted cumulative (50)");
        stale.Status.Should().Be(PaymentStatus.PartiallyRefunded);

        await using var verifyDb = NewDatabase(storeName);
        var persisted = await verifyDb.Set<Payment>().AsNoTracking().SingleAsync(p => p.Id == payment.Id);
        persisted.RefundedAmount.Should().Be(80m);
        persisted.Status.Should().Be(PaymentStatus.PartiallyRefunded);
    }

    // ───────────────────────── N3: gateway amount verification ─────────────────────────

    [Fact]
    public async Task CompleteFromGatewayAsync_MatchingGatewayAmount_Completes()
    {
        var payment = SucceededPayment(50m);
        await using var db = NewDatabase();
        db.Add(payment);
        await db.SaveChangesAsync();
        var refund = PartialRefund(payment, 20m);
        db.Add(refund);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var outcome = await service.CompleteFromGatewayAsync(
            refund, "re_verified_20", "stripe", CancellationToken.None, gatewayReportedAmount: 20m);

        outcome.Success.Should().BeTrue();
        payment.RefundedAmount.Should().Be(20m);
    }

    [Fact]
    public async Task CompleteFromGatewayAsync_GatewayAmountMismatch_FailsClosedWithoutFinalizing()
    {
        var payment = SucceededPayment(50m);
        await using var db = NewDatabase();
        db.Add(payment);
        await db.SaveChangesAsync();
        var refund = PartialRefund(payment, 20m);
        db.Add(refund);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        // The gateway executed the refund for 15 (e.g. after a currency/fee adjustment) while
        // the local row records 20: the completion must NOT finalize the row at full value.
        var outcome = await service.CompleteFromGatewayAsync(
            refund, "re_short_paid", "stripe", CancellationToken.None, gatewayReportedAmount: 15m);

        outcome.Success.Should().BeFalse();
        outcome.FailureReason.Should().Contain("mismatch");
        refund.Status.Should().NotBe(RefundStatus.Completed);
        payment.RefundedAmount.Should().Be(0m);
        payment.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Fact]
    public async Task CompleteFromGatewayAsync_WithoutAGatewayAmount_KeepsLegacyBehavior()
    {
        var payment = SucceededPayment(50m);
        await using var db = NewDatabase();
        db.Add(payment);
        await db.SaveChangesAsync();
        var refund = PartialRefund(payment, 20m);
        db.Add(refund);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var outcome = await service.CompleteFromGatewayAsync(refund, "re_no_amount", "stripe");

        outcome.Success.Should().BeTrue(
            "the coalescing charge.refunded path carries no per-refund amount and stays unverifiable by design");
        payment.RefundedAmount.Should().Be(20m);
    }

    // ───────────────────────── N6: stuck-Pending aging sweep ─────────────────────────

    [Fact]
    public async Task ExitStuckPendingPayments_MarksAgedOutcomeUnknownPaymentsFailedWithReason()
    {
        var payment = Payment.Create(Guid.NewGuid(), 50m, "USD", $"idem-{Guid.NewGuid():N}");
        var repository = new Mock<IPaymentRepository>();
        repository
            .Setup(repo => repo.GetStuckPendingAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync(new[] { payment });
        var worker = new PendingPaymentAgingWorker(
            repository.Object,
            Options.Create(new PaymentAgingOptions { StuckPendingTimeoutHours = 48 }),
            NullLogger<PendingPaymentAgingWorker>.Instance);

        var exited = await worker.ExitStuckPendingPaymentsAsync();

        exited.Should().Be(1);
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.IsRetriable.Should().BeFalse(
            "the charge outcome is unknown; an automated re-charge could double-charge");
        payment.NextRetryAt.Should().BeNull();
        payment.ErrorCode.Should().Be("payment_outcome_unknown_aged");
        payment.FailureReason.Should().Contain("aging sweep");
        repository.Verify(
            repo => repo.UpdateAsync(payment, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExitStuckPendingPayments_NothingStuck_IsANoOp()
    {
        var repository = new Mock<IPaymentRepository>();
        repository
            .Setup(repo => repo.GetStuckPendingAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync(Array.Empty<Payment>());
        var worker = new PendingPaymentAgingWorker(
            repository.Object,
            Options.Create(new PaymentAgingOptions()),
            NullLogger<PendingPaymentAgingWorker>.Instance);

        var exited = await worker.ExitStuckPendingPaymentsAsync();

        exited.Should().Be(0);
        repository.Verify(
            repo => repo.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExitStuckPendingPayments_AFailingPaymentNeverAbortsTheSweep()
    {
        var stuck1 = Payment.Create(Guid.NewGuid(), 50m, "USD", $"idem-{Guid.NewGuid():N}");
        var stuck2 = Payment.Create(Guid.NewGuid(), 50m, "USD", $"idem-{Guid.NewGuid():N}");
        var repository = new Mock<IPaymentRepository>();
        repository
            .Setup(repo => repo.GetStuckPendingAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync(new[] { stuck1, stuck2 });
        var updateCalls = 0;
        repository
            .Setup(repo => repo.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns((Payment p, CancellationToken _) =>
            {
                updateCalls++;
                if (ReferenceEquals(p, stuck1))
                {
                    throw new InvalidOperationException("transient store failure");
                }

                return Task.FromResult(p);
            });
        var worker = new PendingPaymentAgingWorker(
            repository.Object,
            Options.Create(new PaymentAgingOptions()),
            NullLogger<PendingPaymentAgingWorker>.Instance);

        var exited = await worker.ExitStuckPendingPaymentsAsync();

        exited.Should().Be(1);
        updateCalls.Should().Be(2);
        stuck1.Status.Should().Be(PaymentStatus.Pending);
        stuck2.Status.Should().Be(PaymentStatus.Failed);
    }

    // ───────────────────────── N6: candidate query ─────────────────────────

    [Fact]
    public async Task GetStuckPending_SelectsOnlyAgedUnmappedNeverProcessedPendingPayments()
    {
        var stuck = Payment.Create(Guid.NewGuid(), 50m, "USD", $"idem-{Guid.NewGuid():N}");
        stuck.CreatedAt = SystemClock.UtcNow.AddHours(-49);

        var tooRecent = Payment.Create(Guid.NewGuid(), 50m, "USD", $"idem-{Guid.NewGuid():N}");
        tooRecent.CreatedAt = SystemClock.UtcNow.AddHours(-1);

        var mappedAndOld = Payment.Create(Guid.NewGuid(), 50m, "USD", $"idem-{Guid.NewGuid():N}");
        mappedAndOld.CreatedAt = SystemClock.UtcNow.AddHours(-49);
        mappedAndOld.BindProviderMapping("stripe", "test", "acct_platform", "pi_mapped", "payment_intent", "capture");

        var processingAndOld = Payment.Create(Guid.NewGuid(), 50m, "USD", $"idem-{Guid.NewGuid():N}");
        processingAndOld.CreatedAt = SystemClock.UtcNow.AddHours(-49);
        processingAndOld.MarkAsProcessing("pi_processing");

        await using var db = NewDatabase();
        db.AddRange(stuck, tooRecent, mappedAndOld, processingAndOld);
        await db.SaveChangesAsync();
        var repository = new PaymentRepository(
            (IApplicationDbContext)db, NullLogger<PaymentRepository>.Instance);

        var selected = await repository.GetStuckPendingAsync(SystemClock.UtcNow.AddHours(-48));

        selected.Select(p => p.Id).Should().BeEquivalentTo([stuck.Id]);
    }

    // ───────────────────────── helpers ─────────────────────────

    private IRefundProcessingService CreateService(RefundsTestDbContext db) => new RefundProcessingService(
        new RefundRepository((IApplicationDbContext)db, NullLogger<RefundRepository>.Instance),
        new PaymentRepository((IApplicationDbContext)db, NullLogger<PaymentRepository>.Instance),
        new Mock<IPaymentGateway>().Object,
        _revenueAudit.Object,
        _actorAccessor.Object,
        Options.Create(new RefundApprovalOptions()),
        _notifications.Object,
        NullLogger<RefundProcessingService>.Instance);

    private static RefundsTestDbContext NewDatabase(string? storeName = null)
    {
        var options = new DbContextOptionsBuilder<RefundsTestDbContext>()
            .UseInMemoryDatabase(storeName ?? $"refunds-{Guid.NewGuid():N}")
            .Options;
        return new RefundsTestDbContext(options);
    }

    private static Payment SucceededPayment(decimal amount)
    {
        var payment = Payment.Create(Guid.NewGuid(), amount, "USD", $"idem-{Guid.NewGuid():N}");
        payment.MarkAsProcessing();
        payment.MarkAsSucceeded("pi_conflict", "ch_conflict");
        return payment;
    }

    private static Refund PartialRefund(Payment payment, decimal amount)
    {
        var refund = Refund.Create(
            payment.TenantId,
            payment.Id,
            amount,
            "USD",
            "requested_by_customer",
            null,
            Guid.NewGuid(),
            requiresApproval: false,
            $"idem-verify-{Guid.NewGuid():N}",
            kind: RefundKind.Partial);
        return refund;
    }

    private static ActorContext AuthenticatedActor() => new()
    {
        ActorKind = ActorKind.User,
        SubjectId = Guid.NewGuid().ToString(),
        TenantId = Guid.NewGuid(),
        Roles = new HashSet<string>(),
        Permissions = new HashSet<string>(),
        TypedAttributes = ActorAttributes.Empty,
        AuthScheme = "Test",
        IsAuthenticated = true
    };
}
