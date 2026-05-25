using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests;

public class PaymentSubscriptionSyncHandlerTests
{
    [Fact]
    public async Task ProcessPayment_WhenGatewaySucceeds_ShouldSyncSubscription()
    {
        var repository = new Mock<IPaymentRepository>();
        var gateway = new Mock<IPaymentGateway>();
        var syncService = new Mock<IPaymentSubscriptionSyncService>();
        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        gateway.SetupGet(x => x.ProviderId).Returns("stripe");
        repository.Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);
        repository.Setup(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment payment, CancellationToken _) => payment);
        repository.Setup(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment payment, CancellationToken _) => payment);
        gateway.Setup(x => x.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentResult(true, "tx_1", "pay_1", null, null, PaymentStatus.Succeeded, DateTime.UtcNow));

        var handler = new ProcessPaymentCommandHandler(
            repository.Object,
            gateway.Object,
            syncService.Object,
            NullLogger<ProcessPaymentCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessPaymentCommand(tenantId, subscriptionId, 25m, "pm_1"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        syncService.Verify(
            service => service.SyncSuccessfulPaymentAsync(
                It.IsAny<string>(),
                subscriptionId,
                25m,
                "USD",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RetryPayment_WhenGatewaySucceeds_ShouldSyncSubscription()
    {
        var repository = new Mock<IPaymentRepository>();
        var gateway = new Mock<IPaymentGateway>();
        var syncService = new Mock<IPaymentSubscriptionSyncService>();
        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var payment = Payment.Create(tenantId, 40m, "USD", "idempotency", "stripe", subscriptionId, paymentMethodId: "pm_1");
        payment.MarkAsProcessing();
        payment.MarkAsFailed("initial failure");

        repository.Setup(x => x.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        repository.Setup(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment updatedPayment, CancellationToken _) => updatedPayment);
        gateway.Setup(x => x.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentResult(true, "tx_retry", "pay_retry", null, null, PaymentStatus.Succeeded, DateTime.UtcNow));

        var handler = new RetryPaymentCommandHandler(
            repository.Object,
            gateway.Object,
            syncService.Object,
            NullLogger<RetryPaymentCommandHandler>.Instance);

        var result = await handler.Handle(new RetryPaymentCommand(payment.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        syncService.Verify(
            service => service.SyncSuccessfulPaymentAsync(
                payment.Id.ToString(),
                subscriptionId,
                40m,
                "USD",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePaymentStatus_WhenMarkedSucceeded_ShouldSyncSubscription()
    {
        var repository = new Mock<IPaymentRepository>();
        var syncService = new Mock<IPaymentSubscriptionSyncService>();
        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var payment = Payment.Create(tenantId, 15m, "USD", "idempotency", "stripe", subscriptionId);
        payment.MarkAsProcessing();

        repository.Setup(x => x.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        repository.Setup(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment updatedPayment, CancellationToken _) => updatedPayment);

        var handler = new UpdatePaymentStatusCommandHandler(
            repository.Object,
            syncService.Object,
            NullLogger<UpdatePaymentStatusCommandHandler>.Instance);

        var updated = await handler.Handle(
            new UpdatePaymentStatusCommand(payment.Id, PaymentStatus.Succeeded, "tx_status"),
            CancellationToken.None);

        updated.Should().BeTrue();
        syncService.Verify(
            service => service.SyncSuccessfulPaymentAsync(
                payment.Id.ToString(),
                subscriptionId,
                15m,
                "USD",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
