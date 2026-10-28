using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Commands;

/// <summary>
///     Interrupted-refund recovery, retry classification and the composed retry budget
///     (issue #419): a refund stuck in <see cref="RefundStatus.Processing"/> after a crash is
///     reconciled with the provider through its ORIGINAL idempotency key; unsuccessful results
///     keep their <see cref="GatewayFailureKind"/> so permanent declines and outcome-unknown
///     attempts are never replayed by the worker; the provider attempt budget is enforced
///     across the composed execution path; and the confirmation email reaches the intended
///     CUSTOMER recipient even when a support actor requested the refund.
/// </summary>
public class RefundRecoveryAndRetryClassificationTests
{
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly Mock<IRevenueAuditService> _revenueAudit = new();
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly Mock<GameGuild.Notifications.Services.INotificationService> _notifications = new();
    private readonly Mock<IPaymentMethodRepository> _paymentMethods = new();
    private readonly RefundApprovalOptions _options = new();
    private readonly ActorContext _actor;

    public RefundRecoveryAndRetryClassificationTests()
    {
        _actor = AuthenticatedActor();
        _gateway.SetupGet(gateway => gateway.ProviderId).Returns("stripe");
        _actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(_actor);
        _revenueAudit
            .Setup(service => service.RecordRevenueEventAsync(
                It.IsAny<RevenueEventType>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<RevenueSource>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevenueEvent());
        _revenueAudit
            .Setup(service => service.CreateLedgerEntryAsync(
                It.IsAny<LedgerEntryType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinancialLedgerEntry());
        _revenueAudit
            .Setup(service => service.RecordAuditTrailAsync(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notifications
            .Setup(service => service.SendAsync(
                It.IsAny<Guid?>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<NotificationChannel>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Notification.Create(
                Guid.NewGuid(),
                NotificationType.System,
                NotificationChannel.InApp,
                "refund fixture",
                "delivery fixture")));
    }

    [Fact]
    public async Task RecoverInterruptedAsync_CompletesTheRefundUsingTheOriginalIdempotencyKey()
    {
        var payment = SucceededPayment(80m);
        await using var db = NewDatabase();
        db.Add(payment);
        await db.SaveChangesAsync();

        // Crash between the in-flight persist and the gateway outcome (issue #419): the row is
        // stuck in Processing after one recorded execution attempt.
        var refund = Refund.Create(
            payment.TenantId,
            payment.Id,
            80m,
            "USD",
            "goodwill",
            null,
            requestedByUserId: Guid.NewGuid(),
            requiresApproval: false,
            idempotencyKey: $"refund_{payment.Id}_client-key-1");
        db.Add(refund);
        refund.MarkProcessing();
        await db.SaveChangesAsync();

        GatewayRefundRequest? reconciled = null;
        _gateway
            .Setup(gateway => gateway.ProcessRefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()))
            .Callback<GatewayRefundRequest, CancellationToken>((request, _) => reconciled = request)
            .ReturnsAsync(new GatewayRefundResult(
                Success: true,
                RefundId: "re_original_refund",
                AmountRefunded: 80m,
                ErrorCode: null,
                ErrorMessage: null,
                ProcessedAt: SystemClock.UtcNow));

        var service = CreateService(db);
        var outcome = await service.RecoverInterruptedAsync(refund, CancellationToken.None);

        outcome.Success.Should().BeTrue();
        outcome.Refund.Status.Should().Be(RefundStatus.Completed);
        outcome.Refund.GatewayRefundId.Should().Be("re_original_refund");
        outcome.Refund.AttemptCount.Should().Be(1, "recovery reconciles the original attempt; it does not spend a new one");
        payment.RefundedAmount.Should().Be(80m);
        payment.Status.Should().Be(PaymentStatus.Refunded);

        // Provider reconciliation replays the ORIGINAL idempotency key — providers deduplicate
        // the replay, so an attempt that already executed is returned, never duplicated.
        reconciled.Should().NotBeNull();
        reconciled!.IdempotencyKey.Should().Be(refund.IdempotencyKey);
        reconciled.MaxProviderAttempts.Should().Be(3, "attempt 1 of the default 3-attempt budget remains");

        _revenueAudit.Verify(
            audit => audit.RecordRevenueEventAsync(
                RevenueEventType.RefundProcessed,
                80m,
                "USD",
                It.IsAny<RevenueSource>(),
                payment.Id.ToString(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once, "the accounting is emitted exactly once");
    }

    [Fact]
    public async Task RecoverInterruptedAsync_DefinitiveProviderFailure_MarksFailedWithTheClassification()
    {
        var payment = SucceededPayment(40m);
        await using var db = NewDatabase();
        db.Add(payment);
        await db.SaveChangesAsync();
        var refund = NewProcessingRefund(payment);
        db.Add(refund);
        await db.SaveChangesAsync();

        _gateway
            .Setup(gateway => gateway.ProcessRefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayRefundResult(
                Success: false,
                RefundId: null,
                AmountRefunded: 0,
                ErrorCode: "charge_already_refunded",
                ErrorMessage: "The charge was already fully refunded externally.",
                ProcessedAt: SystemClock.UtcNow,
                FailureKind: GatewayFailureKind.TerminalDecline));

        var service = CreateService(db);
        var outcome = await service.RecoverInterruptedAsync(refund, CancellationToken.None);

        outcome.Success.Should().BeFalse();
        outcome.Refund.Status.Should().Be(RefundStatus.Failed);
        outcome.Refund.LastFailureKind.Should().Be(GatewayFailureKind.TerminalDecline,
            "the failure classification survives the recovery path so the worker never replays a permanent decline");
        payment.RefundedAmount.Should().Be(0m);
    }

    [Fact]
    public async Task RecoverInterruptedAsync_OutcomeUnknown_StaysInFlightForWebhookReconciliation()
    {
        var payment = SucceededPayment(40m);
        await using var db = NewDatabase();
        db.Add(payment);
        await db.SaveChangesAsync();
        var refund = NewProcessingRefund(payment);
        db.Add(refund);
        await db.SaveChangesAsync();

        _gateway
            .Setup(gateway => gateway.ProcessRefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayRefundResult(
                Success: false,
                RefundId: null,
                AmountRefunded: 0,
                ErrorCode: "gateway_transport_failure",
                ErrorMessage: "The gateway request never produced a response.",
                ProcessedAt: SystemClock.UtcNow,
                FailureKind: GatewayFailureKind.OutcomeUnknown));

        var service = CreateService(db);
        var outcome = await service.RecoverInterruptedAsync(refund, CancellationToken.None);

        outcome.Success.Should().BeFalse();
        outcome.FailureReason.Should().Contain("webhook reconciliation");
        outcome.Refund.Status.Should().Be(RefundStatus.Processing,
            "an unconfirmable outcome is never marked failed nor replayed; the webhook stays authoritative");
        payment.RefundedAmount.Should().Be(0m);
    }

    [Fact]
    public async Task RecoverInterruptedAsync_ConcurrentWorker_BacksOff_AndTheAccountingIsEmittedOnce()
    {
        var payment = SucceededPayment(60m);
        await using var db = NewDatabase();
        db.Add(payment);
        await db.SaveChangesAsync();
        // A full 60m refund so the reconciled completion refunds the whole payment; the
        // gateway fixture below reports the same 60m refunded at the provider.
        var refund = NewProcessingRefund(payment, amount: 60m);
        db.Add(refund);
        await db.SaveChangesAsync();

        // The first worker's provider call blocks until the test releases it, so the second
        // worker provably overlaps the in-flight reconciliation.
        var gatewayGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gatewayCalls = 0;
        _gateway
            .Setup(gateway => gateway.ProcessRefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref gatewayCalls))
            .Returns(async (GatewayRefundRequest request, CancellationToken cancellationToken) =>
            {
                await gatewayGate.Task.WaitAsync(cancellationToken);
                return new GatewayRefundResult(
                    Success: true,
                    RefundId: "re_single",
                    AmountRefunded: 60m,
                    ErrorCode: null,
                    ErrorMessage: null,
                    ProcessedAt: SystemClock.UtcNow);
            });

        var service = CreateService(db);
        var firstWorker = service.RecoverInterruptedAsync(refund, CancellationToken.None);
        var secondWorker = service.RecoverInterruptedAsync(refund, CancellationToken.None);
        var backedOff = await secondWorker;

        backedOff.Success.Should().BeFalse();
        backedOff.FailureReason.Should().Contain("Another worker");

        gatewayGate.SetResult();
        var first = await firstWorker;
        first.Success.Should().BeTrue();
        first.Refund.Status.Should().Be(RefundStatus.Completed);

        gatewayCalls.Should().Be(1, "the concurrent worker never reached the provider");
        _revenueAudit.Verify(
            audit => audit.RecordRevenueEventAsync(
                RevenueEventType.RefundProcessed,
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<RevenueSource>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        payment.RefundedAmount.Should().Be(60m);
    }

    [Fact]
    public async Task ExecuteAsync_UnsuccessfulResult_PreservesTheFailureKind()
    {
        var payment = SucceededPayment(30m);
        await using var db = NewDatabase();
        db.Add(payment);
        await db.SaveChangesAsync();

        _gateway
            .Setup(gateway => gateway.ProcessRefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayRefundResult(
                Success: false,
                RefundId: null,
                AmountRefunded: 0,
                ErrorCode: "refund_transient_failure",
                ErrorMessage: "transient refund failure",
                ProcessedAt: SystemClock.UtcNow,
                FailureKind: GatewayFailureKind.RetriableGatewayFailure));

        var service = CreateService(db);
        var outcome = await service.RequestRefundAsync(
            new RefundRequestInput(payment.Id, 30m, "goodwill"), CancellationToken.None);

        outcome.Success.Should().BeFalse();
        outcome.Refund!.Status.Should().Be(RefundStatus.Failed);
        outcome.Refund.LastFailureKind.Should().Be(GatewayFailureKind.RetriableGatewayFailure);
    }

    [Fact]
    public async Task ListDueForWorkflowAsync_ExcludesPermanentAndUnknownFailures_AndIncludesStaleProcessing()
    {
        await using var db = NewDatabase();
        var payment = SucceededPayment(200m);
        db.Add(payment);
        await db.SaveChangesAsync();

        var retryable = FailedRefund(payment, GatewayFailureKind.RetriableGatewayFailure);
        var permanent = FailedRefund(payment, GatewayFailureKind.TerminalDecline);
        var unknown = FailedRefund(payment, GatewayFailureKind.OutcomeUnknown);
        var interrupted = NewProcessingRefund(payment, keySuffix: "stuck");
        interrupted.UpdatedAt = SystemClock.UtcNow.AddHours(-1);
        var liveExecution = NewProcessingRefund(payment, keySuffix: "live");
        db.AddRange(retryable, permanent, unknown, interrupted, liveExecution);
        await db.SaveChangesAsync();

        var repository = new RefundRepository((IApplicationDbContext)db, NullLogger<RefundRepository>.Instance);
        var due = await repository.ListDueForWorkflowAsync(
            maxExecutionAttempts: 3,
            failedBeforeUtc: SystemClock.UtcNow.AddHours(2),
            take: 50,
            CancellationToken.None,
            processingStaleBeforeUtc: SystemClock.UtcNow.AddMinutes(-15));

        due.Should().OnlyContain(refund => refund.Id == retryable.Id || refund.Id == interrupted.Id);
        due.Should().NotContain(refund => refund.Id == permanent.Id, "permanent declines are never replayed");
        due.Should().NotContain(refund => refund.Id == unknown.Id, "outcome-unknown attempts belong to reconciliation");
        due.Should().NotContain(refund => refund.Id == liveExecution.Id, "a live in-flight execution is not interrupted");
    }

    [Fact]
    public async Task ExecuteAsync_SendsTheConfirmationToTheCustomer_OwnerOfThePayment()
    {
        var payment = SucceededPayment(55m);
        payment.ApplyPaymentMethodChange("pm_card_visa");
        await using var db = NewDatabase();
        db.Add(payment);
        await db.SaveChangesAsync();

        // The refund is requested by a SUPPORT operator on the customer's behalf; the payment
        // method registration identifies the intended customer recipient (issue #419).
        var customer = Guid.NewGuid();
        var method = new PaymentMethod(payment.TenantId, customer, "stripe", "pm_card_visa", PaymentMethodType.Card, isDefault: true);
        _paymentMethods
            .Setup(methods => methods.GetByGatewayIdAsync(payment.TenantId, It.IsAny<string>(), "pm_card_visa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(method);

        _gateway
            .Setup(gateway => gateway.ProcessRefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayRefundResult(
                Success: true,
                RefundId: "re_ok",
                AmountRefunded: 55m,
                ErrorCode: null,
                ErrorMessage: null,
                ProcessedAt: SystemClock.UtcNow));

        var supportActor = Guid.NewGuid();
        var service = CreateService(db);
        var outcome = await service.RequestRefundAsync(
            new RefundRequestInput(payment.Id, 55m, "goodwill", RequestedByUserId: supportActor),
            CancellationToken.None);

        outcome.Success.Should().BeTrue();
        _notifications.Verify(
            notifications => notifications.SendAsync(
                customer,
                NotificationType.RefundCompleted,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationChannel>(),
                payment.TenantId,
                It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _notifications.VerifyNoOtherCalls();
    }

    private RefundProcessingService CreateService(RefundsTestDbContext db) => new(
        new RefundRepository((IApplicationDbContext)db, NullLogger<RefundRepository>.Instance),
        new PaymentRepository((IApplicationDbContext)db, NullLogger<PaymentRepository>.Instance),
        _gateway.Object,
        _revenueAudit.Object,
        _actorAccessor.Object,
        Options.Create(_options),
        _notifications.Object,
        NullLogger<RefundProcessingService>.Instance,
        paymentMethodRepository: _paymentMethods.Object);

    private RefundsTestDbContext NewDatabase()
    {
        var options = new DbContextOptionsBuilder<RefundsTestDbContext>()
            .UseInMemoryDatabase($"refund-recovery-{Guid.NewGuid():N}")
            .Options;
        return new RefundsTestDbContext(options);
    }

    private static Refund NewProcessingRefund(Payment payment, string keySuffix = "stuck", decimal amount = 10m)
    {
        var refund = Refund.Create(
            payment.TenantId,
            payment.Id,
            amount,
            "USD",
            "goodwill",
            null,
            Guid.NewGuid(),
            requiresApproval: false,
            $"refund_{payment.Id}_{keySuffix}");
        refund.MarkProcessing();
        return refund;
    }

    private static Refund FailedRefund(Payment payment, GatewayFailureKind kind)
    {
        var refund = Refund.Create(
            payment.TenantId,
            payment.Id,
            10m,
            "USD",
            "goodwill",
            null,
            Guid.NewGuid(),
            requiresApproval: false,
            $"refund_{payment.Id}_{Guid.NewGuid():N}");
        refund.MarkProcessing();
        refund.MarkFailed("gateway rejection", kind);
        return refund;
    }

    private static Payment SucceededPayment(decimal amount)
    {
        var payment = Payment.Create(Guid.NewGuid(), amount, "USD", $"idem-{Guid.NewGuid():N}");
        payment.MarkAsProcessing();
        payment.MarkAsSucceeded("pi_1", "ch_1");
        return payment;
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
