using FluentAssertions;
using GameGuild.Commerce;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Commands;

public class ProcessPaymentCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPropagateSubscriptionExternalCustomerIdIntoGatewayRequestAndPaymentRecord()
    {
        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var paymentRepository = new Mock<IPaymentRepository>();
        var paymentGateway = new Mock<IPaymentGateway>();
        var paymentSubscriptionSyncService = new Mock<IPaymentSubscriptionSyncService>();
        var paymentContextService = new Mock<ISubscriptionPaymentContextService>();
        var logger = Mock.Of<ILogger<ProcessPaymentCommandHandler>>();

        Payment? addedPayment = null;

        paymentRepository.Setup(repository => repository.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);
        paymentRepository.Setup(repository => repository.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((payment, _) => addedPayment = payment)
            .ReturnsAsync((Payment payment, CancellationToken _) => payment);
        paymentRepository.Setup(repository => repository.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment payment, CancellationToken _) => payment);

        paymentContextService.Setup(service => service.GetPaymentContextAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionPaymentContext(subscriptionId, tenantId, 25m, "USD", "cus_123"));

        paymentGateway.Setup(gateway => gateway.ProcessPaymentAsync(
                It.Is<GatewayPaymentRequest>(request =>
                    request.CustomerId == "cus_123" &&
                    request.PaymentMethodId == "pm_from_setup"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentResult(
                Success: true,
                TransactionId: "pi_123",
                ExternalPaymentId: "ch_123",
                ErrorCode: null,
                ErrorMessage: null,
                Status: PaymentStatus.Succeeded,
                ProcessedAt: SystemClock.UtcNow));

        paymentSubscriptionSyncService
            .Setup(service => service.SyncSuccessfulPaymentAsync(
                It.IsAny<Guid>(),
                subscriptionId,
                25m,
                "USD",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ProcessPaymentCommandHandler(
            paymentRepository.Object,
            paymentGateway.Object,
            paymentSubscriptionSyncService.Object,
            paymentContextService.Object,
            logger);

        var result = await handler.Handle(
            new ProcessPaymentCommand(tenantId, subscriptionId, 25m, "pm_from_setup"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        addedPayment.Should().NotBeNull();
        addedPayment!.ExternalCustomerId.Should().Be("cus_123");
    }
}