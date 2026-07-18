using FluentAssertions;
using GameGuild.Commerce;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Services;

public sealed class OrderPaymentServiceTests
{
    [Fact]
    public async Task ProcessAsync_ShouldCreatePaymentFromAuthoritativeOrderFacts()
    {
        var repository = new Mock<IPaymentRepository>();
        repository.Setup(repo => repo.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);
        repository.Setup(repo => repo.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment payment, CancellationToken _) => payment);
        repository.Setup(repo => repo.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment payment, CancellationToken _) => payment);

        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(item => item.ProviderId).Returns("stripe");
        gateway.Setup(item => item.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentResult(
                true,
                "txn_order",
                "pi_order",
                null,
                null,
                PaymentStatus.Succeeded,
                SystemClock.UtcNow));

        var service = new OrderPaymentService(
            repository.Object,
            gateway.Object,
            Mock.Of<ILogger<OrderPaymentService>>());
        var charge = new AuthoritativeOrderCharge(
            Guid.NewGuid(),
            Guid.NewGuid(),
            75m,
            "EUR",
            "pm_order");

        var result = await service.ProcessAsync(charge);

        result.Success.Should().BeTrue();
        result.PaymentId.Should().NotBeNull();
        repository.Verify(repo => repo.AddAsync(
            It.Is<Payment>(payment =>
                payment.OrderId == charge.OrderId &&
                payment.TenantId == charge.TenantId &&
                payment.Amount == 75m &&
                payment.Currency == "EUR" &&
                payment.SubscriptionId == null),
            It.IsAny<CancellationToken>()), Times.Once);
        gateway.Verify(item => item.ProcessPaymentAsync(
            It.Is<GatewayPaymentRequest>(request =>
                request.Amount == 75m &&
                request.Currency == "EUR" &&
                request.Metadata!["order_id"] == charge.OrderId.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsSettledAsync_ShouldRejectAnyPaymentBindingMismatch()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var payment = Payment.Create(
            tenantId,
            75m,
            "USD",
            "order-payment",
            orderId: orderId);
        payment.MarkAsProcessing();
        payment.MarkAsSucceeded("pi_order", "txn_order");

        var repository = new Mock<IPaymentRepository>();
        repository.Setup(repo => repo.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        var service = new OrderPaymentService(
            repository.Object,
            Mock.Of<IPaymentGateway>(),
            Mock.Of<ILogger<OrderPaymentService>>());

        var exact = await service.IsSettledAsync(new OrderPaymentBinding(orderId, payment.Id, tenantId, 75m, "USD"));
        var wrongAmount = await service.IsSettledAsync(new OrderPaymentBinding(orderId, payment.Id, tenantId, 1m, "USD"));
        var wrongTenant = await service.IsSettledAsync(new OrderPaymentBinding(orderId, payment.Id, Guid.NewGuid(), 75m, "USD"));

        exact.Should().BeTrue();
        wrongAmount.Should().BeFalse();
        wrongTenant.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAsync_ShouldReturnExistingSuccessfulPaymentWithoutChargingAgain()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var charge = new AuthoritativeOrderCharge(orderId, tenantId, 75m, "USD", "pm_order");
        var payment = Payment.Create(
            tenantId,
            charge.Amount,
            charge.Currency,
            OrderPaymentService.CreateIdempotencyKey(tenantId, orderId),
            orderId: orderId,
            paymentMethodId: charge.PaymentMethodId);
        payment.MarkAsProcessing();
        payment.MarkAsSucceeded("pi_existing", "txn_existing");

        var repository = new Mock<IPaymentRepository>();
        repository.Setup(repo => repo.GetByIdempotencyKeyAsync(payment.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        var gateway = new Mock<IPaymentGateway>();
        var service = new OrderPaymentService(
            repository.Object,
            gateway.Object,
            Mock.Of<ILogger<OrderPaymentService>>());

        var result = await service.ProcessAsync(charge);

        result.Success.Should().BeTrue();
        result.PaymentId.Should().Be(payment.Id);
        result.ExternalPaymentId.Should().Be("pi_existing");
        gateway.VerifyNoOtherCalls();
        repository.Verify(repo => repo.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldRejectIdempotencyKeyBoundToDifferentSnapshot()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var existing = Payment.Create(
            tenantId,
            75m,
            "USD",
            OrderPaymentService.CreateIdempotencyKey(tenantId, orderId),
            orderId: orderId,
            paymentMethodId: "pm_order");
        var repository = new Mock<IPaymentRepository>();
        repository.Setup(repo => repo.GetByIdempotencyKeyAsync(existing.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var gateway = new Mock<IPaymentGateway>();
        var service = new OrderPaymentService(
            repository.Object,
            gateway.Object,
            Mock.Of<ILogger<OrderPaymentService>>());

        var action = () => service.ProcessAsync(
            new AuthoritativeOrderCharge(orderId, tenantId, 1m, "USD", "pm_order"));

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*authoritative order snapshot*");
        gateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessAsync_ShouldPersistGatewayFailureAndReturnFailedResult()
    {
        Payment? persisted = null;
        var repository = new Mock<IPaymentRepository>();
        repository.Setup(repo => repo.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);
        repository.Setup(repo => repo.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment payment, CancellationToken _) => payment);
        repository.Setup(repo => repo.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((payment, _) => persisted = payment)
            .ReturnsAsync((Payment payment, CancellationToken _) => payment);
        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(item => item.ProviderId).Returns("stripe");
        gateway.Setup(item => item.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentResult(
                false,
                null,
                null,
                "card_declined",
                "Card declined",
                PaymentStatus.Failed,
                SystemClock.UtcNow));
        var service = new OrderPaymentService(
            repository.Object,
            gateway.Object,
            Mock.Of<ILogger<OrderPaymentService>>());

        var result = await service.ProcessAsync(
            new AuthoritativeOrderCharge(Guid.NewGuid(), Guid.NewGuid(), 75m, "USD", "pm_order"));

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("Card declined");
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(PaymentStatus.Failed);
        persisted.ErrorCode.Should().Be("card_declined");
    }
}
