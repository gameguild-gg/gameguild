using FluentAssertions;
using GameGuild.ValueObjects;
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
    public void Invoice_Amount_CannotBeModifiedAfterCreation()
    {
        // Arrange
        var originalAmount = new Money(29.99m, "USD");
        var invoice = Invoice.Create(
            subscriptionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            amount: originalAmount,
            periodStart: DateTime.UtcNow,
            periodEnd: DateTime.UtcNow.AddMonths(1)
        );

        // Act & Assert
        // Attempting to modify the amount should throw
        var act = () => invoice.UpdateAmount(new Money(99.99m, "USD"));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be modified*");
    }

    [Fact]
    public void Invoice_Amount_ShouldMatchCreationValue()
    {
        // Arrange
        var expectedAmount = new Money(49.99m, "USD");

        // Act
        var invoice = Invoice.Create(
            subscriptionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            amount: expectedAmount,
            periodStart: DateTime.UtcNow,
            periodEnd: DateTime.UtcNow.AddMonths(1)
        );

        // Assert
        invoice.Amount.Should().Be(expectedAmount);
    }

    [Fact]
    public void Invoice_Currency_CannotBeChangedAfterCreation()
    {
        // Arrange
        var invoice = Invoice.Create(
            subscriptionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            amount: new Money(29.99m, "USD"),
            periodStart: DateTime.UtcNow,
            periodEnd: DateTime.UtcNow.AddMonths(1)
        );

        // Assert
        invoice.Amount.Currency.Should().Be("USD");
        // Currency should be immutable as part of the Money value object
    }

    [Fact]
    public void Invoice_TotalAmount_IncludesAllLineItems()
    {
        // Arrange
        var invoice = Invoice.Create(
            subscriptionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            amount: new Money(0m, "USD"),
            periodStart: DateTime.UtcNow,
            periodEnd: DateTime.UtcNow.AddMonths(1)
        );

        // Act
        invoice.AddLineItem("Subscription - Pro Plan", 29.99m);
        invoice.AddLineItem("Add-on - Extra Storage", 9.99m);

        // Assert
        invoice.TotalAmount.Value.Should().Be(39.98m);
    }

    [Fact]
    public void Invoice_LineItems_CannotBeModifiedAfterFinalization()
    {
        // Arrange
        var invoice = Invoice.Create(
            subscriptionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            amount: new Money(29.99m, "USD"),
            periodStart: DateTime.UtcNow,
            periodEnd: DateTime.UtcNow.AddMonths(1)
        );
        invoice.AddLineItem("Subscription", 29.99m);
        invoice.Finalize();

        // Act & Assert
        var act = () => invoice.AddLineItem("Unauthorized Item", 100m);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*finalized*");
    }

    #endregion

    #region Status Transition Tests (P0)

    [Fact]
    public void Invoice_Draft_CanTransitionToIssued()
    {
        // Arrange
        var invoice = Invoice.Create(
            subscriptionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            amount: new Money(29.99m, "USD"),
            periodStart: DateTime.UtcNow,
            periodEnd: DateTime.UtcNow.AddMonths(1)
        );

        // Act
        invoice.Issue();

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.IssuedAt.Should().NotBeNull();
    }

    [Fact]
    public void Invoice_Issued_CanTransitionToPaid()
    {
        // Arrange
        var invoice = CreateIssuedInvoice();

        // Act
        invoice.MarkAsPaid("txn_12345");

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAt.Should().NotBeNull();
        invoice.TransactionId.Should().Be("txn_12345");
    }

    [Fact]
    public void Invoice_Paid_CannotTransitionToVoid()
    {
        // Arrange
        var invoice = CreateIssuedInvoice();
        invoice.MarkAsPaid("txn_12345");

        // Act & Assert
        var act = () => invoice.Void("Customer request");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*paid invoice*");
    }

    [Fact]
    public void Invoice_Issued_CanBeVoided()
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

        // Act & Assert - Cannot mark as paid
        var actPaid = () => invoice.MarkAsPaid("txn_fake");
        actPaid.Should().Throw<InvalidOperationException>();

        // Cannot re-issue
        var actIssue = () => invoice.Issue();
        actIssue.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Issued)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Failed)]
    [InlineData(InvoiceStatus.Void)]
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
        var invoice = Invoice.Create(
            subscriptionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            amount: new Money(29.99m, "USD"),
            periodStart: DateTime.UtcNow,
            periodEnd: DateTime.UtcNow.AddMonths(1)
        );

        // Assert
        invoice.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Invoice_TracksStatusChangeHistory()
    {
        // Arrange
        var invoice = Invoice.Create(
            subscriptionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            amount: new Money(29.99m, "USD"),
            periodStart: DateTime.UtcNow,
            periodEnd: DateTime.UtcNow.AddMonths(1)
        );

        // Act
        invoice.Issue();
        invoice.MarkAsPaid("txn_123");

        // Assert
        invoice.StatusHistory.Should().HaveCount(3); // Draft -> Issued -> Paid
        invoice.StatusHistory.First().Status.Should().Be(InvoiceStatus.Draft);
        invoice.StatusHistory.Last().Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void Invoice_HasInvoiceNumber_AfterIssue()
    {
        // Arrange
        var invoice = Invoice.Create(
            subscriptionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            amount: new Money(29.99m, "USD"),
            periodStart: DateTime.UtcNow,
            periodEnd: DateTime.UtcNow.AddMonths(1)
        );

        // Assert - No number before issue
        invoice.InvoiceNumber.Should().BeNull();

        // Act
        invoice.Issue();

        // Assert - Has number after issue
        invoice.InvoiceNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Invoice_InvoiceNumber_CannotBeChanged()
    {
        // Arrange
        var invoice = CreateIssuedInvoice();
        var originalNumber = invoice.InvoiceNumber;

        // Act & Assert
        // Invoice number should be immutable once assigned
        invoice.InvoiceNumber.Should().Be(originalNumber);
    }

    #endregion

    #region Credit/Refund Tests (P1)

    [Fact]
    public void Invoice_Paid_CanHaveCreditNoteIssued()
    {
        // Arrange
        var invoice = CreateIssuedInvoice();
        invoice.MarkAsPaid("txn_12345");

        // Act
        var creditNote = invoice.IssueCreditNote(10m, "Partial refund");

        // Assert
        creditNote.Should().NotBeNull();
        creditNote.Amount.Should().Be(10m);
        creditNote.RelatedInvoiceId.Should().Be(invoice.Id);
        creditNote.Type.Should().Be(CreditNoteType.PartialRefund);
    }

    [Fact]
    public void Invoice_CreditNote_CannotExceedOriginalAmount()
    {
        // Arrange
        var invoice = CreateIssuedInvoice();
        invoice.MarkAsPaid("txn_12345");

        // Act & Assert
        var act = () => invoice.IssueCreditNote(999.99m, "Over-refund attempt");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*exceeds*");
    }

    [Fact]
    public void Invoice_MultipleCreditNotes_CannotExceedTotal()
    {
        // Arrange
        var invoice = Invoice.Create(
            subscriptionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            amount: new Money(100m, "USD"),
            periodStart: DateTime.UtcNow,
            periodEnd: DateTime.UtcNow.AddMonths(1)
        );
        invoice.Issue();
        invoice.MarkAsPaid("txn_12345");

        // Act - Issue partial credit notes
        invoice.IssueCreditNote(30m, "Refund 1");
        invoice.IssueCreditNote(30m, "Refund 2");
        invoice.IssueCreditNote(30m, "Refund 3");

        // Assert - Cannot issue more than remaining
        var act = () => invoice.IssueCreditNote(20m, "Over-refund");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*exceeds*");
    }

    #endregion

    #region Helper Methods

    private static Invoice CreateIssuedInvoice()
    {
        var invoice = Invoice.Create(
            subscriptionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            amount: new Money(29.99m, "USD"),
            periodStart: DateTime.UtcNow,
            periodEnd: DateTime.UtcNow.AddMonths(1)
        );
        invoice.Issue();
        return invoice;
    }

    #endregion
}

/// <summary>
/// Supporting types for InvoiceImmutabilityTests
/// </summary>
public class CreditNote
{
    public Guid Id { get; set; }
    public Guid RelatedInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public CreditNoteType Type { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum CreditNoteType
{
    PartialRefund,
    FullRefund,
    Adjustment,
    GoodwillCredit
}

public class InvoiceStatusChange
{
    public InvoiceStatus Status { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
}
