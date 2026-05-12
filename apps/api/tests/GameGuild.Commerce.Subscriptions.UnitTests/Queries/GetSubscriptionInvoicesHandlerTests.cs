using FluentAssertions;
using GameGuild.Commerce.Payments;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Queries;

public class GetSubscriptionInvoicesHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmptyPage_WhenSubscriptionDoesNotExist()
    {
        // Arrange
        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Set<Subscription>()).Returns(Array.Empty<Subscription>().AsQueryable().BuildMockDbSet().Object);
        context.Setup(c => c.Set<SubscriptionInvoiceReadModel>()).Returns(Array.Empty<SubscriptionInvoiceReadModel>().AsQueryable().BuildMockDbSet().Object);
        context.Setup(c => c.Set<Payment>()).Returns(Array.Empty<Payment>().AsQueryable().BuildMockDbSet().Object);

        var handler = new GetSubscriptionInvoicesHandler(context.Object);

        // Act
        var result = await handler.Handle(new GetSubscriptionInvoicesQuery(Guid.NewGuid(), 2, 10), CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Skip.Should().Be(10);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedInvoices_WhenSubscriptionExists()
    {
        // Arrange
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(29.99m, "USD"),
            startDate: DateTime.UtcNow);

        var payment = Payment.Create(
            tenantId: Guid.NewGuid(),
            amount: 29.99m,
            currency: "USD",
            idempotencyKey: Guid.NewGuid().ToString("N"),
            paymentMethodId: "pm_card_visa",
            subscriptionId: subscription.Id);

        var invoice = new SubscriptionInvoiceReadModel
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            InvoiceNumber = "INV-1001",
            Total = 29.99m,
            Currency = "USD",
            CreatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedAt = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
            DueDate = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            PaidAt = new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc),
            Status = 2,
            PaymentId = payment.Id,
            ExternalId = "ext-invoice-1"
        };

        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Set<Subscription>()).Returns(new[] { subscription }.AsQueryable().BuildMockDbSet().Object);
        context.Setup(c => c.Set<SubscriptionInvoiceReadModel>()).Returns(new[] { invoice }.AsQueryable().BuildMockDbSet().Object);
        context.Setup(c => c.Set<Payment>()).Returns(new[] { payment }.AsQueryable().BuildMockDbSet().Object);

        var handler = new GetSubscriptionInvoicesHandler(context.Object);

        // Act
        var result = await handler.Handle(new GetSubscriptionInvoicesQuery(subscription.Id), CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();

        var item = result.Items.Single();
        item.Id.Should().Be(invoice.Id);
        item.SubscriptionId.Should().Be(subscription.Id);
        item.InvoiceNumber.Should().Be("INV-1001");
        item.Amount.Should().Be(29.99m);
        item.Currency.Should().Be("USD");
        item.InvoiceDate.Should().Be(invoice.IssuedAt);
        item.DueDate.Should().Be(invoice.DueDate);
        item.PaidDate.Should().Be(invoice.PaidAt);
        item.Status.Should().Be("Paid");
        item.PaymentMethod.Should().Be("pm_card_visa");
        item.ExternalInvoiceId.Should().Be("ext-invoice-1");
    }
}
