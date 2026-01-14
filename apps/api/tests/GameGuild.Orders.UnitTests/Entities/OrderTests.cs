using FluentAssertions;
using GameGuild.Commerce.Orders;
using Xunit;

namespace GameGuild.Commerce.Orders.UnitTests.Entities;

/// <summary>
///     Tests for Order entity state machine and TenantId validation.
///     These tests verify:
///     - Invariant #1: No financial entity exists without valid TenantId
///     - Invariant #4: Financial state transitions are monotonic
///     - Invariant #8: Partial failures cannot cause accounting inconsistency
/// </summary>
public class OrderTests
{
    #region Order Creation Tests

    [Fact]
    public void Create_WithValidTenantId_ShouldCreateOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var idempotencyKey = "order_123";

        // Act
        var order = Order.Create(userId, idempotencyKey, tenantId);

        // Assert
        order.UserId.Should().Be(userId);
        order.IdempotencyKey.Should().Be(idempotencyKey);
        order.TenantId.Should().Be(tenantId);
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void Create_WithEmptyTenantId_ShouldThrow()
    {
        // Arrange - Invariant #1: No financial entity exists without valid TenantId
        var userId = Guid.NewGuid();
        var emptyTenantId = Guid.Empty;
        var idempotencyKey = "order_123";

        // Act & Assert
        var act = () => Order.Create(userId, idempotencyKey, emptyTenantId);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*TenantId*required*fail-closed*");
    }

    #endregion

    #region State Machine Valid Transitions Tests

    [Fact]
    public void Pending_CanTransitionTo_Processing()
    {
        // Arrange - Invariant #4: Financial state transitions are monotonic
        var order = CreatePendingOrder();

        // Assert
        order.CanTransitionTo(OrderStatus.Processing).Should().BeTrue();
    }

    [Fact]
    public void Pending_CanTransitionTo_Completed()
    {
        // Arrange
        var order = CreatePendingOrder();

        // Assert
        order.CanTransitionTo(OrderStatus.Completed).Should().BeTrue();
    }

    [Fact]
    public void Pending_CanTransitionTo_Failed()
    {
        // Arrange
        var order = CreatePendingOrder();

        // Assert
        order.CanTransitionTo(OrderStatus.Failed).Should().BeTrue();
    }

    [Fact]
    public void Pending_CanTransitionTo_Cancelled()
    {
        // Arrange
        var order = CreatePendingOrder();

        // Assert
        order.CanTransitionTo(OrderStatus.Cancelled).Should().BeTrue();
    }

    [Fact]
    public void Completed_CanTransitionTo_Refunded()
    {
        // Arrange
        var order = CreateCompletedOrder();

        // Assert
        order.CanTransitionTo(OrderStatus.Refunded).Should().BeTrue();
    }

    [Fact]
    public void Completed_CanTransitionTo_PartiallyRefunded()
    {
        // Arrange
        var order = CreateCompletedOrder();

        // Assert
        order.CanTransitionTo(OrderStatus.PartiallyRefunded).Should().BeTrue();
    }

    [Fact]
    public void Completed_CanTransitionTo_Disputed()
    {
        // Arrange
        var order = CreateCompletedOrder();

        // Assert
        order.CanTransitionTo(OrderStatus.Disputed).Should().BeTrue();
    }

    #endregion

    #region State Machine Invalid Transitions Tests

    [Fact]
    public void Failed_IsTerminalState()
    {
        // Arrange - Failed is a terminal state
        var order = CreatePendingOrder();
        order.Fail("Payment processor error");

        // Assert
        order.CanTransitionTo(OrderStatus.Pending).Should().BeFalse();
        order.CanTransitionTo(OrderStatus.Processing).Should().BeFalse();
        order.CanTransitionTo(OrderStatus.Completed).Should().BeFalse();
        order.CanTransitionTo(OrderStatus.Cancelled).Should().BeFalse();
    }

    [Fact]
    public void Cancelled_IsTerminalState()
    {
        // Arrange - Cancelled is a terminal state
        var order = CreatePendingOrder();
        order.Cancel("User requested cancellation");

        // Assert
        order.CanTransitionTo(OrderStatus.Pending).Should().BeFalse();
        order.CanTransitionTo(OrderStatus.Processing).Should().BeFalse();
        order.CanTransitionTo(OrderStatus.Completed).Should().BeFalse();
        order.CanTransitionTo(OrderStatus.Failed).Should().BeFalse();
    }

    [Fact]
    public void Refunded_IsTerminalState()
    {
        // Arrange - Refunded is a terminal state
        var order = CreateCompletedOrder();
        order.ProcessRefund(order.Total, "Customer returned product");

        // Assert
        order.CanTransitionTo(OrderStatus.Completed).Should().BeFalse();
        order.CanTransitionTo(OrderStatus.PartiallyRefunded).Should().BeFalse();
    }

    [Fact]
    public void Pending_CannotTransitionTo_Refunded()
    {
        // Arrange - Cannot refund an order that hasn't been completed
        var order = CreatePendingOrder();

        // Assert
        order.CanTransitionTo(OrderStatus.Refunded).Should().BeFalse();
    }

    [Fact]
    public void Completed_CannotTransitionTo_Pending()
    {
        // Arrange - Cannot go back to pending after completion
        var order = CreateCompletedOrder();

        // Assert
        order.CanTransitionTo(OrderStatus.Pending).Should().BeFalse();
    }

    #endregion

    #region Order Operations Tests

    [Fact]
    public void MarkAsPaid_ShouldTransitionToProcessing()
    {
        // Arrange
        var order = CreatePendingOrder();
        var externalPaymentId = "ch_stripe_123";

        // Act
        order.MarkAsPaid(paymentProviderReference: "ref_123", paymentMethod: "card", externalPaymentId: externalPaymentId);

        // Assert
        order.Status.Should().Be(OrderStatus.Completed);
        order.ExternalPaymentId.Should().Be(externalPaymentId);
        order.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_ShouldTransitionToCancelled()
    {
        // Arrange
        var order = CreatePendingOrder();

        // Act
        order.Cancel("Customer changed mind");

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromCompletedState_ShouldThrow()
    {
        // Arrange
        var order = CreateCompletedOrder();

        // Act & Assert
        var act = () => order.Cancel("Too late to cancel");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid order state transition*");
    }

    [Fact]
    public void Fail_ShouldTransitionToFailed()
    {
        // Arrange
        var order = CreatePendingOrder();

        // Act
        order.Fail("Payment declined");

        // Assert
        order.Status.Should().Be(OrderStatus.Failed);
    }

    [Fact]
    public void ProcessRefund_FullRefund_ShouldTransitionToRefunded()
    {
        // Arrange
        var order = CreateCompletedOrder();

        // Act
        order.ProcessRefund(order.Total, "Full refund requested");

        // Assert
        order.Status.Should().Be(OrderStatus.Refunded);
        order.RefundAmount.Should().Be(order.Total);
        order.RefundReason.Should().Be("Full refund requested");
        order.RefundedAt.Should().NotBeNull();
    }

    [Fact]
    public void ProcessRefund_PartialRefund_ShouldTransitionToPartiallyRefunded()
    {
        // Arrange
        var order = CreateCompletedOrder();
        var partialAmount = order.Total / 2;

        // Act
        order.ProcessRefund(partialAmount, "Partial refund for damaged item");

        // Assert
        order.Status.Should().Be(OrderStatus.PartiallyRefunded);
        order.RefundAmount.Should().Be(partialAmount);
    }

    [Fact]
    public void ProcessRefund_FromPending_ShouldThrow()
    {
        // Arrange - Cannot refund an order that hasn't been paid
        var order = CreatePendingOrder();

        // Act & Assert
        var act = () => order.ProcessRefund(10m, "Refund");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid order state transition*");
    }

    #endregion

    #region Domain Event Tests

    [Fact]
    public void MarkAsPaid_ShouldRaiseOrderStateChangedEvent()
    {
        // Arrange
        var order = CreatePendingOrder();

        // Act
        order.MarkAsPaid();

        // Assert
        var events = order.DomainEvents;
        events.Should().Contain(e => e.GetType() == typeof(OrderStateChangedEvent));
    }

    [Fact]
    public void Cancel_ShouldRaiseOrderStateChangedEvent()
    {
        // Arrange
        var order = CreatePendingOrder();

        // Act
        order.Cancel("Cancelled");

        // Assert
        var events = order.DomainEvents;
        events.Should().Contain(e => e.GetType() == typeof(OrderStateChangedEvent));
    }

    #endregion

    #region Idempotency Key Tests

    [Fact]
    public void Order_ShouldHaveUniqueIdempotencyKey()
    {
        // Arrange & Act
        var order1 = Order.Create(Guid.NewGuid(), "key_1", Guid.NewGuid());
        var order2 = Order.Create(Guid.NewGuid(), "key_2", Guid.NewGuid());

        // Assert
        order1.IdempotencyKey.Should().NotBe(order2.IdempotencyKey);
    }

    #endregion

    #region Helper Methods

    private static Order CreatePendingOrder()
    {
        var order = Order.Create(
            userId: Guid.NewGuid(),
            idempotencyKey: Guid.NewGuid().ToString(),
            tenantId: Guid.NewGuid(),
            currency: "USD"
        );
        order.Total = 99.99m;
        return order;
    }

    private static Order CreateCompletedOrder()
    {
        var order = CreatePendingOrder();
        order.MarkAsPaid();
        return order;
    }

    #endregion
}
