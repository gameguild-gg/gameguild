using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using GameGuild.Commerce.Payments;
using GameGuild.Commerce.Subscriptions;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

public sealed class SubscriptionChangeSagaOrchestratorTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid FromPlanId = Guid.NewGuid();
    private static readonly Guid ToPlanId = Guid.NewGuid();
    private static readonly Guid PaymentMethodRegistryId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    private readonly Mock<IApplicationDbContext> _context = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<ISubscriptionPlanService> _planService = new();
    private readonly Mock<ISubscriptionIntervalPricingService> _intervalPricing = new();
    private readonly Mock<IBillingCalculator> _billingCalculator = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IPaymentMethodRepository> _paymentMethodRepository = new();
    private readonly Mock<IBillingIntegrationEventPublisher> _billingEvents = new();

    private readonly List<SubscriptionChangeSaga> _sagas = [];
    private readonly List<SubscriptionChangeSagaStep> _sagaSteps = [];
    private readonly List<Invoice> _invoices = [];
    private readonly IGatewayPaymentMethodIdProtector _protector =
        new DataProtectionGatewayPaymentMethodIdProtector(new EphemeralDataProtectionProvider());

    public SubscriptionChangeSagaOrchestratorTests()
    {
        var sagaSet = _sagas.AsQueryable().BuildMockDbSet();
        sagaSet.Setup(set => set.Add(It.IsAny<SubscriptionChangeSaga>()))
            .Callback<SubscriptionChangeSaga>(saga => _sagas.Add(saga));
        var stepSet = _sagaSteps.AsQueryable().BuildMockDbSet();
        stepSet.Setup(set => set.Add(It.IsAny<SubscriptionChangeSagaStep>()))
            .Callback<SubscriptionChangeSagaStep>(step => _sagaSteps.Add(step));
        var invoiceSet = _invoices.AsQueryable().BuildMockDbSet();
        invoiceSet.Setup(set => set.Add(It.IsAny<Invoice>()))
            .Callback<Invoice>(invoice => _invoices.Add(invoice));
        _context
            .Setup(context => context.Set<SubscriptionChangeSaga>())
            .Returns(sagaSet.Object);
        _context
            .Setup(context => context.Set<SubscriptionChangeSagaStep>())
            .Returns(stepSet.Object);
        _context
            .Setup(context => context.Set<Invoice>())
            .Returns(invoiceSet.Object);
        _context
            .Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _planService
            .Setup(service => service.GetByIdAsync(ToPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionPlan("Pro", "pro", 20000, "USD"));

        _intervalPricing
            .Setup(service => service.ResolvePriceForCycleAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<BillingCycle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Money(200m, "USD"));

        _billingCalculator
            .Setup(calculator => calculator.CalculateProration(
                It.IsAny<Money>(), It.IsAny<Money>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new PlanChangeProration(CreditForUnused: 100m, ChargeForNew: 200m, NetAdjustment: 100m, EffectiveDate: SystemClock.UtcNow));

        _paymentMethodRepository
            .Setup(repository => repository.GetByIdAsync(PaymentMethodRegistryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentMethod(TenantId, ActorId, "stripe", "pm_stripe_1", GameGuild.Commerce.Payments.PaymentMethodType.Card, true));

        _billingEvents
            .Setup(publisher => publisher.PublishSubscriptionChangeStepAsync(
                It.IsAny<SubscriptionChangeSaga>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _billingEvents
            .Setup(publisher => publisher.PublishSubscriptionChangeCompletedAsync(It.IsAny<SubscriptionChangeSaga>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _billingEvents
            .Setup(publisher => publisher.PublishSubscriptionChangeFailedAsync(It.IsAny<SubscriptionChangeSaga>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private Subscription NewActiveSubscription()
    {
        var subscription = new Subscription(
            TenantId,
            FromPlanId,
            ActorId,
            BillingCycle.Monthly,
            new Money(100m, "USD"),
            SystemClock.UtcNow.AddMonths(-1));
        subscription.Activate();
        subscription.SetPaymentMethod(PaymentMethodRegistryId);
        return subscription;
    }

    private SubscriptionChangeSagaOrchestrator NewOrchestrator() => new(
        _context.Object,
        _subscriptionRepository.Object,
        _planService.Object,
        _intervalPricing.Object,
        _billingCalculator.Object,
        _paymentGateway.Object,
        _paymentMethodRepository.Object,
        _billingEvents.Object,
        NullLogger<SubscriptionChangeSagaOrchestrator>.Instance,
        _protector);

    [Fact]
    public async Task ExecuteAsync_EncryptedMethod_IsDecryptedBeforeCharging()
    {
        var subscription = NewActiveSubscription();
        SetupSubscription(subscription);
        SetupGatewayChargeSuccess();
        var method = new PaymentMethod(TenantId, ActorId, "stripe", "pm_stripe_1", GameGuild.Commerce.Payments.PaymentMethodType.Card, true);
        method.StoreProtectedGatewayReference(_protector.Protect("pm_stripe_1"), GatewayPaymentMethodIdHash.Compute("stripe", "pm_stripe_1"));
        _paymentMethodRepository.Setup(repository => repository.GetByIdAsync(PaymentMethodRegistryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(method);

        var result = await NewOrchestrator().ExecuteAsync(new SubscriptionChangeSagaRequest(subscription.Id, ToPlanId, null, null, ActorId));

        result.Succeeded.Should().BeTrue();
        _paymentGateway.Verify(gateway => gateway.ProcessPaymentAsync(
            It.Is<GatewayPaymentRequest>(request => request.PaymentMethodId == "pm_stripe_1" && request.GatewayProvider == "stripe"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AnotherOwnersMethod_DoesNotCharge()
    {
        var subscription = NewActiveSubscription();
        SetupSubscription(subscription);
        _paymentMethodRepository.Setup(repository => repository.GetByIdAsync(PaymentMethodRegistryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentMethod(TenantId, Guid.NewGuid(), "stripe", "pm_other_owner", GameGuild.Commerce.Payments.PaymentMethodType.Card, true));

        var result = await NewOrchestrator().ExecuteAsync(new SubscriptionChangeSagaRequest(subscription.Id, ToPlanId, null, null, ActorId));

        result.Succeeded.Should().BeFalse();
        _paymentGateway.Verify(gateway => gateway.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupGatewayChargeSuccess()
    {
        _paymentGateway
            .Setup(gateway => gateway.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentResult(
                Success: true,
                TransactionId: "txn_saga_1",
                ExternalPaymentId: "ch_saga_1",
                ErrorCode: null,
                ErrorMessage: null,
                Status: GameGuild.Commerce.Payments.PaymentStatus.Succeeded,
                ProcessedAt: SystemClock.UtcNow));
    }

    private void SetupSubscription(Subscription subscription)
    {
        _subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _subscriptionRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_CompletesAllStepsAndActivatesNewPlan()
    {
        var subscription = NewActiveSubscription();
        SetupSubscription(subscription);
        SetupGatewayChargeSuccess();
        var orchestrator = NewOrchestrator();

        var result = await orchestrator.ExecuteAsync(
            new SubscriptionChangeSagaRequest(subscription.Id, ToPlanId, null, null, ActorId));

        result.Succeeded.Should().BeTrue();
        result.Status.Should().Be(SubscriptionChangeSagaStatus.Completed);
        result.FromPlanId.Should().Be(FromPlanId);
        result.ToPlanId.Should().Be(ToPlanId);
        result.NetAdjustment.Should().Be(100m);
        result.PaymentTransactionId.Should().Be("txn_saga_1");
        subscription.PlanId.Should().Be(ToPlanId, "the activation step moves the subscription to the new plan");

        result.Steps.Select(step => (step.Step, step.Outcome)).Should().Equal(
            ("Proration", "Succeeded"),
            ("Invoice", "Succeeded"),
            ("Payment", "Succeeded"),
            ("Activation", "Succeeded"));

        _invoices.Should().ContainSingle().Which.Status.Should().Be(InvoiceStatus.Paid, "the charge settles the issued invoice");
        _billingEvents.Verify(publisher => publisher.PublishSubscriptionChangeCompletedAsync(
            It.Is<SubscriptionChangeSaga>(saga => saga.ToPlanId == ToPlanId && saga.NetAdjustment == 100m),
            It.IsAny<CancellationToken>()), Times.Once);
        _billingEvents.Verify(publisher => publisher.PublishSubscriptionChangeStepAsync(
            It.IsAny<SubscriptionChangeSaga>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(4));
        _billingEvents.Verify(publisher => publisher.PublishSubscriptionChangeFailedAsync(
            It.IsAny<SubscriptionChangeSaga>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PaymentFails_VoidsInvoiceAndFailsWithoutRefund()
    {
        var subscription = NewActiveSubscription();
        SetupSubscription(subscription);
        _paymentGateway
            .Setup(gateway => gateway.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentResult(
                Success: false,
                TransactionId: null,
                ExternalPaymentId: null,
                ErrorCode: "card_declined",
                ErrorMessage: "card declined",
                Status: GameGuild.Commerce.Payments.PaymentStatus.Failed,
                ProcessedAt: SystemClock.UtcNow));

        var result = await NewOrchestrator().ExecuteAsync(
            new SubscriptionChangeSagaRequest(subscription.Id, ToPlanId, null, null, ActorId));

        result.Succeeded.Should().BeFalse();
        result.Status.Should().Be(SubscriptionChangeSagaStatus.Failed);
        result.FailedStep.Should().Be("Payment");
        result.CompensationsApplied.Should().Contain("invoice-voided");
        subscription.PlanId.Should().Be(FromPlanId, "the subscription keeps its original plan");
        _invoices.Should().ContainSingle().Which.Status.Should().Be(InvoiceStatus.Void);
        _paymentGateway.Verify(gateway => gateway.ProcessRefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()), Times.Never,
            "no charge succeeded, so there is nothing to refund");
        _billingEvents.Verify(publisher => publisher.PublishSubscriptionChangeFailedAsync(
            It.Is<SubscriptionChangeSaga>(saga => saga.FailedStep == "Payment"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ActivationFails_RefundsChargeAndKeepsInvoicePaid()
    {
        var subscription = NewActiveSubscription();
        _subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _subscriptionRepository
            .SetupSequence(repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("concurrent modification detected"))
            .ReturnsAsync(subscription);
        SetupGatewayChargeSuccess();
        _paymentGateway
            .Setup(gateway => gateway.ProcessRefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayRefundResult(
                Success: true,
                RefundId: "re_saga_1",
                AmountRefunded: 100m,
                ErrorCode: null,
                ErrorMessage: null,
                ProcessedAt: SystemClock.UtcNow));

        var result = await NewOrchestrator().ExecuteAsync(
            new SubscriptionChangeSagaRequest(subscription.Id, ToPlanId, null, null, ActorId));

        result.Succeeded.Should().BeFalse();
        result.FailedStep.Should().Be("Activation");
        result.CompensationsApplied.Should().Contain("refund:re_saga_1");
        _paymentGateway.Verify(gateway => gateway.ProcessRefundAsync(
            It.Is<GatewayRefundRequest>(request =>
                request.OriginalTransactionId == "txn_saga_1"
                && request.Amount == 100m
                && request.ProviderId == "stripe"
                && request.Currency == "USD"
                && request.IdempotencyKey.Contains("refund")),
            It.IsAny<CancellationToken>()), Times.Once);
        _invoices.Should().ContainSingle().Which.Status.Should().Be(InvoiceStatus.Paid,
            "a paid invoice cannot be voided; the refund is the compensation");
        _billingEvents.Verify(publisher => publisher.PublishSubscriptionChangeFailedAsync(
            It.Is<SubscriptionChangeSaga>(saga => saga.FailedStep == "Activation"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NetCreditChange_SkipsInvoiceAndPaymentAndActivates()
    {
        var subscription = NewActiveSubscription();
        SetupSubscription(subscription);
        _billingCalculator
            .Setup(calculator => calculator.CalculateProration(
                It.IsAny<Money>(), It.IsAny<Money>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new PlanChangeProration(CreditForUnused: 200m, ChargeForNew: 100m, NetAdjustment: -100m, EffectiveDate: SystemClock.UtcNow));

        var result = await NewOrchestrator().ExecuteAsync(
            new SubscriptionChangeSagaRequest(subscription.Id, ToPlanId, null, null, ActorId));

        result.Succeeded.Should().BeTrue();
        result.NetAdjustment.Should().Be(-100m);
        result.InvoiceId.Should().BeNull();
        subscription.PlanId.Should().Be(ToPlanId);
        _invoices.Should().BeEmpty();
        _paymentGateway.Verify(gateway => gateway.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        result.Steps.Should().Contain(step => step.Step == "Invoice" && step.Outcome == "Skipped");
    }

    [Fact]
    public async Task ExecuteAsync_ReplayedIntent_ResolvesRecordedSagaWithoutRecharging()
    {
        var subscription = NewActiveSubscription();
        SetupSubscription(subscription);
        SetupGatewayChargeSuccess();
        var orchestrator = NewOrchestrator();
        var request = new SubscriptionChangeSagaRequest(subscription.Id, ToPlanId, new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc), null, ActorId);

        var first = await orchestrator.ExecuteAsync(request);
        var second = await orchestrator.ExecuteAsync(request);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        second.SagaId.Should().Be(first.SagaId, "the replay resolves to the recorded saga");
        second.Steps.Should().Equal(first.Steps);
        _sagas.Should().ContainSingle();
        _paymentGateway.Verify(gateway => gateway.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Once,
            "the idempotency key prevents a double charge");
        _billingEvents.Verify(publisher => publisher.PublishSubscriptionChangeCompletedAsync(
            It.IsAny<SubscriptionChangeSaga>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_MissingTargetPlan_FailsAtProrationWithNoSideEffects()
    {
        var subscription = NewActiveSubscription();
        SetupSubscription(subscription);
        _planService
            .Setup(service => service.GetByIdAsync(ToPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        var result = await NewOrchestrator().ExecuteAsync(
            new SubscriptionChangeSagaRequest(subscription.Id, ToPlanId, null, null, ActorId));

        result.Succeeded.Should().BeFalse();
        result.FailedStep.Should().Be("Proration");
        _invoices.Should().BeEmpty();
        subscription.PlanId.Should().Be(FromPlanId);
        _billingEvents.Verify(publisher => publisher.PublishSubscriptionChangeFailedAsync(
            It.IsAny<SubscriptionChangeSaga>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_MissingPaymentMethodForPositiveNet_FailsAtPaymentWithVoid()
    {
        var subscription = NewActiveSubscription();
        subscription.SetPaymentMethod(null);
        SetupSubscription(subscription);
        _paymentMethodRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentMethod?)null);

        var result = await NewOrchestrator().ExecuteAsync(
            new SubscriptionChangeSagaRequest(subscription.Id, ToPlanId, null, null, ActorId));

        result.Succeeded.Should().BeFalse();
        result.FailedStep.Should().Be("Payment");
        result.CompensationsApplied.Should().Contain("invoice-voided");
        _paymentGateway.Verify(gateway => gateway.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_SamePlanRequest_IsRejectedAsDomainViolation()
    {
        var subscription = NewActiveSubscription();
        SetupSubscription(subscription);

        var act = () => NewOrchestrator().ExecuteAsync(
            new SubscriptionChangeSagaRequest(subscription.Id, FromPlanId, null, null, ActorId));

        await act.Should().ThrowAsync<GameGuild.BusinessRuleViolationException>();
    }
}
