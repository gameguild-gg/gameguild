using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Security;

/// <summary>
///     P0/P1 Critical Tests: Invoice Immutability
///     From: COMMERCE_MODULES_SECURITY_AUDIT.md Section 7 - Test Plan
///     These tests verify that invoice amounts cannot be modified after creation.
/// </summary>
public class InvoiceImmutabilityTests
{
    #region Amount Immutability Tests (P0)

    [Fact]
    public void Invoice_Amount_CannotBeModifiedAfterIssuance()
    {
        // Arrange
        var invoice = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: 29.99m,
            currency: "USD"
        );
        invoice.Issue();

        // Act & Assert - Attempting to apply discount after issue should throw
        var act = () => invoice.ApplyDiscount(5m);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*immutable*");
    }

    [Fact]
    public void Invoice_Amount_ShouldMatchCreationValue()
    {
        // Arrange
        var expectedAmount = 49.99m;

        // Act
        var invoice = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: expectedAmount,
            currency: "USD"
        );

        // Assert
        invoice.Subtotal.Should().Be(expectedAmount);
        invoice.Total.Should().Be(expectedAmount);
    }

    [Fact]
    public void Invoice_Currency_IsSetOnCreation()
    {
        // Arrange & Act
        var invoice = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: 29.99m,
            currency: "USD"
        );

        // Assert
        invoice.Currency.Should().Be("USD");
    }

    [Fact]
    public void Invoice_TotalAmount_IncludesDiscountAndTax()
    {
        // Arrange
        var invoice = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: 100m,
            currency: "USD"
        );

        // Act
        invoice.ApplyDiscount(10m);
        invoice.SetTax(5m);

        // Assert - Total = Subtotal - Discount + Tax = 100 - 10 + 5 = 95
        invoice.Total.Should().Be(95m);
    }

    [Fact]
    public void Invoice_Discount_CannotBeAppliedAfterIssuance()
    {
        // Arrange
        var invoice = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: 29.99m,
            currency: "USD"
        );
        invoice.Issue();

        // Act & Assert
        var act = () => invoice.ApplyDiscount(5m);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*immutable*");
    }

    #endregion

    #region Status Transition Tests (P0)

    [Fact]
    public void Invoice_Draft_CanTransitionToOpen()
    {
        // Arrange
        var invoice = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: 29.99m,
            currency: "USD"
        );

        // Act
        invoice.Issue();

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Open);
        invoice.IssuedAt.Should().NotBeNull();
    }

    [Fact]
    public void Invoice_Open_CanTransitionToPaid()
    {
        // Arrange
        var invoice = CreateIssuedInvoice();
        var paymentId = Guid.NewGuid();

        // Act
        invoice.RecordPayment(paymentId, invoice.Total, DateTime.UtcNow);

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAt.Should().NotBeNull();
        invoice.PaymentId.Should().Be(paymentId);
    }

    [Fact]
    public void Invoice_Paid_CannotTransitionToVoid()
    {
        // Arrange
        var invoice = CreateIssuedInvoice();
        invoice.RecordPayment(Guid.NewGuid(), invoice.Total, DateTime.UtcNow);

        // Act & Assert
        var act = () => invoice.Void("Customer request");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*paid invoice*");
    }

    [Fact]
    public void Invoice_Open_CanBeVoided()
    {
        // Arrange
        var invoice = CreateIssuedInvoice();

        // Act
        invoice.Void("Duplicate invoice");

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Void);
        invoice.VoidedAt.Should().NotBeNull();
        invoice.VoidReason.Should().Be("Duplicate invoice");
    }

    [Fact]
    public void Invoice_Void_CannotTransitionToAnyOtherStatus()
    {
        // Arrange
        var invoice = CreateIssuedInvoice();
        invoice.Void("Test void");

        // Act & Assert - Cannot record payment
        var actPaid = () => invoice.RecordPayment(Guid.NewGuid(), invoice.Total, DateTime.UtcNow);
        actPaid.Should().Throw<InvalidOperationException>();

        // Cannot re-issue
        var actIssue = () => invoice.Issue();
        actIssue.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Open)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.PastDue)]
    [InlineData(InvoiceStatus.Void)]
    [InlineData(InvoiceStatus.Uncollectible)]
    public void Invoice_AllStatusValues_AreValid(InvoiceStatus status)
    {
        // Assert - Enum values are defined
        Enum.IsDefined(typeof(InvoiceStatus), status).Should().BeTrue();
    }

    #endregion

    #region Audit Trail Tests (P1)

    [Fact]
    public void Invoice_CreatedAt_IsSetOnCreation()
    {
        // Arrange & Act
        var invoice = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: 29.99m,
            currency: "USD"
        );

        // Assert
        invoice.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Invoice_IsImmutable_AfterIssuance()
    {
        // Arrange
        var invoice = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: 29.99m,
            currency: "USD"
        );

        // Assert - Not immutable while draft
        invoice.IsImmutable.Should().BeFalse();

        // Act
        invoice.Issue();

        // Assert - Immutable after issue
        invoice.IsImmutable.Should().BeTrue();
    }

    [Fact]
    public void Invoice_HasInvoiceNumber_OnCreation()
    {
        // Arrange & Act
        var invoice = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: 29.99m,
            currency: "USD"
        );

        // Assert - Has number on creation
        invoice.InvoiceNumber.Should().NotBeNullOrEmpty();
        invoice.InvoiceNumber.Should().StartWith("INV-");
    }

    [Fact]
    public void Invoice_InvoiceNumber_IsUnique()
    {
        // Arrange & Act
        var invoice1 = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: 29.99m,
            currency: "USD"
        );
        var invoice2 = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: 29.99m,
            currency: "USD"
        );

        // Assert
        invoice1.InvoiceNumber.Should().NotBe(invoice2.InvoiceNumber);
    }

    #endregion

    #region Payment Tests (P1)

    [Fact]
    public void Invoice_Draft_CannotReceivePayment()
    {
        // Arrange
        var invoice = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: 29.99m,
            currency: "USD"
        );

        // Act & Assert
        var act = () => invoice.RecordPayment(Guid.NewGuid(), 29.99m, DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*draft*");
    }

    [Fact]
    public void Invoice_SinglePaymentPerInvoice_IsEnforced()
    {
        // Arrange
        var invoice = CreateIssuedInvoice();
        var firstPaymentId = Guid.NewGuid();
        var secondPaymentId = Guid.NewGuid();
        invoice.RecordPayment(firstPaymentId, invoice.Total / 2, DateTime.UtcNow);

        // Act & Assert - Cannot use different payment ID
        var act = () => invoice.RecordPayment(secondPaymentId, invoice.Total / 2, DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Single payment*");
    }

    [Fact]
    public void Invoice_AmountRemaining_CalculatesCorrectly()
    {
        // Arrange
        var invoice = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: 100m,
            currency: "USD"
        );
        invoice.Issue();

        // Assert - Initial amount remaining
        invoice.AmountRemaining.Should().Be(100m);

        // Act - Partial payment
        var paymentId = Guid.NewGuid();
        invoice.RecordPayment(paymentId, 40m, DateTime.UtcNow);

        // Assert - Remaining after partial payment
        invoice.AmountRemaining.Should().Be(60m);
    }

    #endregion

    #region Helper Methods

    private static Invoice CreateIssuedInvoice()
    {
        var invoice = new Invoice(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            amount: 29.99m,
            currency: "USD"
        );
        invoice.Issue();
        return invoice;
    }

    #endregion
}
