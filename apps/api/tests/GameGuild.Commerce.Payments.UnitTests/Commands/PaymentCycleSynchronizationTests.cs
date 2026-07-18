using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Commands;

public sealed class PaymentCycleSynchronizationTests
{
    [Fact]
    public async Task RetryPayment_ShouldSynchronizeOriginalBillingCycle()
    {
        var payment = CreatePaymentForCycle(3);
        payment.MarkAsProcessing();
        payment.MarkAsFailed("temporary failure");

        var repository = CreateRepository(payment);
        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(item => item.ProviderId).Returns("stripe");
        var syncService = new Mock<IPaymentSubscriptionSyncService>();
        gateway
            .Setup(service => service.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessfulGatewayResult());

        var handler = new RetryPaymentCommandHandler(
            repository.Object,
            gateway.Object,
            syncService.Object,
            Mock.Of<ILogger<RetryPaymentCommandHandler>>());

        var result = await handler.Handle(new RetryPaymentCommand(payment.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        syncService.Verify(
            service => service.SyncSuccessfulPaymentAsync(
                payment.Id,
                payment.SubscriptionId,
                payment.Amount,
                payment.Currency,
                3,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePaymentStatus_ShouldSynchronizeOriginalBillingCycle()
    {
        var payment = CreatePaymentForCycle(4);
        payment.MarkAsProcessing();

        var repository = CreateRepository(payment);
        var syncService = new Mock<IPaymentSubscriptionSyncService>();
        var handler = new UpdatePaymentStatusCommandHandler(
            repository.Object,
            syncService.Object,
            Mock.Of<ILogger<UpdatePaymentStatusCommandHandler>>());

        var result = await handler.Handle(
            new UpdatePaymentStatusCommand(payment.Id, PaymentStatus.Succeeded, "pi_123"),
            CancellationToken.None);

        result.Should().BeTrue();
        syncService.Verify(
            service => service.SyncSuccessfulPaymentAsync(
                payment.Id,
                payment.SubscriptionId,
                payment.Amount,
                payment.Currency,
                4,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RetryPayment_ShouldNotSynchronizeSubscription_WhenBillingCycleIdentityIsMissing()
    {
        var payment = CreatePayment("legacy-payment-key");
        payment.MarkAsProcessing();
        payment.MarkAsFailed("temporary failure");

        var repository = CreateRepository(payment);
        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(item => item.ProviderId).Returns("stripe");
        var syncService = new Mock<IPaymentSubscriptionSyncService>();
        gateway
            .Setup(service => service.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessfulGatewayResult());
        var handler = new RetryPaymentCommandHandler(
            repository.Object,
            gateway.Object,
            syncService.Object,
            Mock.Of<ILogger<RetryPaymentCommandHandler>>());

        var result = await handler.Handle(new RetryPaymentCommand(payment.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        syncService.Verify(
            service => service.SyncSuccessfulPaymentAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdatePaymentStatus_ShouldNotSynchronizeSubscription_WhenBillingCycleIdentityIsMissing()
    {
        var payment = CreatePayment("legacy-payment-key");
        payment.MarkAsProcessing();

        var repository = CreateRepository(payment);
        var syncService = new Mock<IPaymentSubscriptionSyncService>();
        var handler = new UpdatePaymentStatusCommandHandler(
            repository.Object,
            syncService.Object,
            Mock.Of<ILogger<UpdatePaymentStatusCommandHandler>>());

        var result = await handler.Handle(
            new UpdatePaymentStatusCommand(payment.Id, PaymentStatus.Succeeded, "pi_123"),
            CancellationToken.None);

        result.Should().BeTrue();
        syncService.Verify(
            service => service.SyncSuccessfulPaymentAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Payment CreatePaymentForCycle(int billingCycle)
    {
        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var key = $"subscription:{tenantId:N}:{subscriptionId:N}:cycle:{billingCycle}:charge";

        return CreatePayment(key, tenantId, subscriptionId);
    }

    private static Payment CreatePayment(
        string idempotencyKey,
        Guid? tenantId = null,
        Guid? subscriptionId = null)
    {
        return Payment.Create(
            tenantId ?? Guid.NewGuid(),
            25m,
            "USD",
            idempotencyKey,
            subscriptionId: subscriptionId ?? Guid.NewGuid(),
            paymentMethodId: "pm_test");
    }

    private static Mock<IPaymentRepository> CreateRepository(Payment payment)
    {
        var repository = new Mock<IPaymentRepository>();
        repository
            .Setup(service => service.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        repository
            .Setup(service => service.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment updatedPayment, CancellationToken _) => updatedPayment);

        return repository;
    }

    private static GatewayPaymentResult SuccessfulGatewayResult()
    {
        return new GatewayPaymentResult(
            Success: true,
            TransactionId: "pi_123",
            ExternalPaymentId: "ch_123",
            ErrorCode: null,
            ErrorMessage: null,
            Status: PaymentStatus.Succeeded,
            ProcessedAt: SystemClock.UtcNow,
            ProviderMapping: new GatewayProviderMapping(
                "test",
                "acct_platform",
                "pi_123",
                "payment_intent",
                "capture"));
    }
}
