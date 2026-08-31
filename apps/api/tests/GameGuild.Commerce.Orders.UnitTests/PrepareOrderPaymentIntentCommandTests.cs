using FluentAssertions;
using GameGuild.Commerce;
using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Orders.UnitTests;

public sealed class PrepareOrderPaymentIntentCommandTests
{
    [Fact]
    public async Task Handle_RejectsMissingUnauthorizedAndInvalidOrders()
    {
        var repository = new Mock<IOrderRepository>();
        var preparer = new Mock<IOrderPaymentIntentPreparer>();
        var unauthorizedOrder = PayableOrder();
        var invalidStatusOrder = CancelledOrder();
        var notPayableOrder = OrderTestFactory.CreatePendingOrder();
        var handler = new PrepareOrderPaymentIntentCommandHandler(repository.Object, preparer.Object, OrderTestFactory.CreateActor());
        repository.SetupSequence(item => item.GetWithLineItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null)
            .ReturnsAsync(unauthorizedOrder)
            .ReturnsAsync(invalidStatusOrder)
            .ReturnsAsync(notPayableOrder);

        var missing = await handler.Handle(new PrepareOrderPaymentIntentCommand(Guid.NewGuid()), default);
        var unauthorized = await handler.Handle(new PrepareOrderPaymentIntentCommand(Guid.NewGuid()), default);
        var invalidStatus = await new PrepareOrderPaymentIntentCommandHandler(
            repository.Object, preparer.Object, OrderTestFactory.CreateActor(invalidStatusOrder))
            .Handle(new PrepareOrderPaymentIntentCommand(Guid.NewGuid()), default);
        var notPayable = await new PrepareOrderPaymentIntentCommandHandler(
            repository.Object, preparer.Object, OrderTestFactory.CreateActor(notPayableOrder))
            .Handle(new PrepareOrderPaymentIntentCommand(Guid.NewGuid()), default);

        missing.IsFailure.Should().BeTrue();
        unauthorized.IsFailure.Should().BeTrue();
        invalidStatus.IsFailure.Should().BeTrue();
        notPayable.IsFailure.Should().BeTrue();
        preparer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_ReservesPendingOrderAndUsesOnlyAuthoritativeFacts()
    {
        var order = PayableOrder();
        var repository = Repository(order);
        var preparer = new Mock<IOrderPaymentIntentPreparer>();
        var preparation = new OrderPaymentIntentPreparation(true, Guid.NewGuid(), "pi_secret", null, OrderChargeState.RequiresAction);
        preparer.Setup(item => item.PrepareAsync(It.IsAny<AuthoritativeOrderPaymentIntent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(preparation);
        var handler = new PrepareOrderPaymentIntentCommandHandler(
            repository.Object, preparer.Object, OrderTestFactory.CreateActor(order));

        var result = await handler.Handle(new PrepareOrderPaymentIntentCommand(order.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(preparation);
        order.Status.Should().Be(OrderStatus.Processing);
        repository.Verify(item => item.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        preparer.Verify(item => item.PrepareAsync(
            It.Is<AuthoritativeOrderPaymentIntent>(intent =>
                intent.OrderId == order.Id && intent.TenantId == order.TenantId &&
                intent.Amount == order.Total && intent.Currency == order.Currency),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Provider unavailable.")]
    [InlineData(null)]
    public async Task Handle_MapsPreparationFailureWithoutReleasingReservation(string? reason)
    {
        var order = PayableOrder();
        order.StartPaymentProcessing();
        var repository = Repository(order);
        var preparer = new Mock<IOrderPaymentIntentPreparer>();
        preparer.Setup(item => item.PrepareAsync(It.IsAny<AuthoritativeOrderPaymentIntent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPaymentIntentPreparation(false, null, null, reason, OrderChargeState.RequiresReconciliation));
        var handler = new PrepareOrderPaymentIntentCommandHandler(
            repository.Object, preparer.Object, OrderTestFactory.CreateActor(order));

        var result = await handler.Handle(new PrepareOrderPaymentIntentCommand(order.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Be(reason ?? "PaymentIntent unavailable.");
        order.Status.Should().Be(OrderStatus.Processing);
        repository.Verify(item => item.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IOrderRepository> Repository(Order order)
    {
        var repository = new Mock<IOrderRepository>();
        repository.Setup(item => item.GetWithLineItemsAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        return repository;
    }

    private static Order PayableOrder()
    {
        var order = OrderTestFactory.CreatePendingOrder();
        OrderTestFactory.AddLineItem(order, Guid.NewGuid(), "Product", 25m);
        return order;
    }

    private static Order CancelledOrder()
    {
        var order = PayableOrder();
        order.Cancel("cancelled");
        return order;
    }
}
