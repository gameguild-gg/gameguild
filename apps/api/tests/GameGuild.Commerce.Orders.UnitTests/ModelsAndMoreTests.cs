using FluentAssertions;
using System.Linq;
using Xunit;

namespace GameGuild.Commerce.Orders.UnitTests;

public class OrderLineItemTests
{
    [Fact]
    public void Constructor_IsNotPublic()
    {
        typeof(OrderLineItem).GetConstructors().Should().BeEmpty();
    }

    [Fact]
    public void PricingProperties_ShouldBeCapturedByOrder()
    {
        var order = Order.Create(Guid.NewGuid(), "snapshot-properties", Guid.NewGuid());
        var snapshot = new OrderLineItemPricingSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), 3, 39.99m, 29.99m, 29.99m, "USD");

        var item = order.AddLineItem(
            Guid.NewGuid(),
            "Product A",
            snapshot,
            quantity: 3,
            discountAmount: 5m,
            promoCodesApplied: "[\"SAVE10\"]",
            pricingTierName: "Premium",
            isSubscription: true);

        item.ProductNameSnapshot.Should().Be("Product A");
        item.UnitPriceSnapshot.Should().Be(29.99m);
        item.ProductPricingId.Should().Be(snapshot.ProductPricingId);
        item.ProductPricingVersionId.Should().Be(snapshot.ProductPricingVersionId);
        item.PriceVersionSnapshot.Should().Be(3);
        item.CurrencySnapshot.Should().Be("USD");
        item.Quantity.Should().Be(3);
        item.IsSubscription.Should().BeTrue();
        item.PricingTierNameSnapshot.Should().Be("Premium");
    }
}

public class OrderAuditLogTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var log = new OrderAuditLog();

        log.Reason.Should().BeNull();
        log.ExternalPaymentId.Should().BeNull();
        log.InitiatedBy.Should().BeNull();
        log.IpAddress.Should().BeNull();
        log.AdditionalContext.Should().BeNull();
    }

    [Fact]
    public void FromEvent_ShouldMapAllProperties()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var evt = new OrderStateChangedEvent(
            orderId,
            tenantId,
            OrderStatus.Pending,
            OrderStatus.Paid,
            "Payment received",
            "ext-pay-123");

        var log = OrderAuditLog.FromEvent(evt, "admin@test.com", "192.168.1.1", "{\"source\":\"api\"}");

        log.OrderId.Should().Be(orderId);
        log.TenantId.Should().Be(tenantId);
        log.PreviousStatus.Should().Be(OrderStatus.Pending);
        log.NewStatus.Should().Be(OrderStatus.Paid);
        log.Reason.Should().Be("Payment received");
        log.ExternalPaymentId.Should().Be("ext-pay-123");
        log.InitiatedBy.Should().Be("admin@test.com");
        log.IpAddress.Should().Be("192.168.1.1");
        log.AdditionalContext.Should().Be("{\"source\":\"api\"}");
        log.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void FromEvent_WithoutOptionals_ShouldSetSystemDefaults()
    {
        var evt = new OrderStateChangedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrderStatus.Pending,
            OrderStatus.Cancelled);

        var log = OrderAuditLog.FromEvent(evt);

        log.InitiatedBy.Should().Be("System");
        log.IpAddress.Should().BeNull();
        log.AdditionalContext.Should().BeNull();
    }
}

public class OrderStateChangedEventTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var evt = new OrderStateChangedEvent(
            orderId,
            tenantId,
            OrderStatus.Pending,
            OrderStatus.Completed,
            "Payment confirmed",
            "pay-ext-1");

        evt.OrderId.Should().Be(orderId);
        evt.TenantId.Should().Be(tenantId);
        evt.PreviousStatus.Should().Be(OrderStatus.Pending);
        evt.NewStatus.Should().Be(OrderStatus.Completed);
        evt.Reason.Should().Be("Payment confirmed");
        evt.ExternalPaymentId.Should().Be("pay-ext-1");
        evt.EventId.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithoutOptionals_ShouldDefaultToNull()
    {
        var evt = new OrderStateChangedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrderStatus.Pending,
            OrderStatus.Failed);

        evt.Reason.Should().BeNull();
        evt.ExternalPaymentId.Should().BeNull();
    }
}

public class OrderEnumsTests
{
    [Theory]
    [InlineData(OrderType.OneTimePurchase, 0)]
    [InlineData(OrderType.Subscribe, 1)]
    [InlineData(OrderType.Upgrade, 2)]
    [InlineData(OrderType.Downgrade, 3)]
    [InlineData(OrderType.AddOn, 4)]
    [InlineData(OrderType.Renewal, 5)]
    public void OrderType_ShouldHaveExpectedValues(OrderType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, 0)]
    [InlineData(OrderStatus.Processing, 1)]
    [InlineData(OrderStatus.Completed, 2)]
    [InlineData(OrderStatus.Failed, 3)]
    [InlineData(OrderStatus.Cancelled, 4)]
    [InlineData(OrderStatus.Refunded, 5)]
    [InlineData(OrderStatus.PartiallyRefunded, 6)]
    [InlineData(OrderStatus.Disputed, 7)]
    [InlineData(OrderStatus.Paid, 8)]
    [InlineData(OrderStatus.Fulfilled, 9)]
    [InlineData(OrderStatus.OnHold, 10)]
    public void OrderStatus_ShouldHaveExpectedValues(OrderStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }
}

public class OrderOperationResultTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var order = CreateTestOrder();

        var result = new OrderOperationResult(order);

        result.Order.Should().BeSameAs(order);
        result.WasDuplicate.Should().BeFalse();
    }

    [Fact]
    public void FromOrder_ShouldCreateInstance()
    {
        var order = CreateTestOrder();

        var result = OrderOperationResult.FromOrder(order, true);

        result.Order.Should().BeSameAs(order);
        result.WasDuplicate.Should().BeTrue();
    }

    private static Order CreateTestOrder() =>
        Order.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), Guid.NewGuid());
}

public class OrderAdditionalMethodTests
{
    [Fact]
    public void AddLineItem_ShouldAddAndRecalculate()
    {
        var order = CreateTestOrder();

        var item = OrderTestFactory.AddLineItem(order, Guid.NewGuid(), "Product A", 25.00m, 2, 5.00m);

        item.ProductNameSnapshot.Should().Be("Product A");
        item.UnitPriceSnapshot.Should().Be(25.00m);
        item.Quantity.Should().Be(2);
        item.DiscountAmount.Should().Be(5.00m);
        item.LineTotal.Should().Be(45.00m);
        order.LineItems.Should().Contain(item);
        order.Subtotal.Should().Be(50.00m);
    }

    [Fact]
    public void RecalculateTotals_WhenCompleted_ShouldThrow()
    {
        var order = CreateTestOrder();
        order.MarkAsPaid();

        var act = () => order.RecalculateTotals();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PlaceOnHold_ShouldTransitionToOnHold()
    {
        var order = CreateTestOrder();

        order.PlaceOnHold("Fraud review");

        order.Status.Should().Be(OrderStatus.OnHold);
        order.Metadata.Should().Be("Fraud review");
    }

    [Fact]
    public void Release_ShouldTransitionBackToPending()
    {
        var order = CreateTestOrder();
        order.PlaceOnHold("Review");

        order.Release();

        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void SoftDelete_ShouldSetDeletedAt()
    {
        var order = CreateTestOrder();

        order.SoftDelete();

        order.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void IsSuccessfullyCompleted_WhenFulfilled_ShouldBeTrue()
    {
        var order = CreateTestOrder();
        order.MarkAsPaidPendingFulfillment(Guid.NewGuid(), "ext-1");
        order.MarkAsFulfilled();

        order.IsSuccessfullyCompleted.Should().BeTrue();
    }

    [Fact]
    public void IsSuccessfullyCompleted_WhenPending_ShouldBeFalse()
    {
        CreateTestOrder().IsSuccessfullyCompleted.Should().BeFalse();
    }

    [Fact]
    public void IsSuccessfullyCompleted_WhenLegacyCompletedWithoutFulfillment_ShouldBeFalse()
    {
        var order = CreateTestOrder();
        order.MarkAsPaid("provider-ref", "card", "ext-legacy-false");

        order.IsSuccessfullyCompleted.Should().BeFalse();
    }

    [Fact]
    public void IsSuccessfullyCompleted_WhenLegacyCompletedAndFulfilled_ShouldBeTrue()
    {
        var order = CreateTestOrder();
        order.MarkAsPaid("provider-ref", "card", "ext-legacy-true");
        order.MarkAsFulfilled();

        order.IsSuccessfullyCompleted.Should().BeTrue();
    }

    [Fact]
    public void AssociatePayment_ShouldEnforceSinglePaymentInvariant()
    {
        var order = CreateTestOrder();
        var paymentId = Guid.NewGuid();
        order.AssociatePayment(paymentId);

        order.PaymentId.Should().Be(paymentId);

        var act = () => order.AssociatePayment(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();

        order.AssociatePayment(paymentId);
        order.PaymentId.Should().Be(paymentId);
    }

    [Fact]
    public void AssociatePayment_ShouldAllowMatchingPaymentAssignedDuringPaymentFlow()
    {
        var order = CreateTestOrder();
        var paymentId = Guid.NewGuid();
        order.MarkAsPaidPendingFulfillment(paymentId, "ext-2");

        order.AssociatePayment(paymentId);

        order.PaymentId.Should().Be(paymentId);
    }

    [Fact]
    public void TransitionEvents_ShouldUseGuidEmptyWhenTenantIsMissing()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };

        order.Cancel("missing tenant");

        var events = order.DomainEvents.OfType<OrderStateChangedEvent>().ToList();
        events.Should().HaveCount(2);
        events.Should().OnlyContain(evt => evt.TenantId == Guid.Empty);
    }

    [Fact]
    public void MarkAsFulfilled_AlreadyFulfilled_ShouldBeIdempotent()
    {
        var order = CreateTestOrder();
        order.MarkAsPaidPendingFulfillment(Guid.NewGuid());
        order.MarkAsFulfilled();
        var fulfilledAt = order.FulfilledAt;

        order.MarkAsFulfilled();

        order.FulfilledAt.Should().Be(fulfilledAt);
    }

    private static Order CreateTestOrder() =>
        Order.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), Guid.NewGuid());
}
