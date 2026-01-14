using FluentAssertions;
using GameGuild.Commerce.Payments;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Entities;

/// <summary>
///     Tests for FinancialLedgerEntry immutability after reconciliation.
///     These tests verify that once reconciled, entries cannot be modified.
/// </summary>
public class FinancialLedgerEntryTests
{
    #region Reconciliation Tests

    [Fact]
    public void Reconcile_ShouldSetReconciliationFields()
    {
        // Arrange
        var entry = CreateLedgerEntry();
        var userId = Guid.NewGuid();

        // Act
        entry.Reconcile(userId, "Monthly reconciliation");

        // Assert
        entry.IsReconciled.Should().BeTrue();
        entry.ReconciledAt.Should().NotBeNull();
        entry.ReconciledBy.Should().Be(userId);
        entry.Notes.Should().Be("Monthly reconciliation");
    }

    [Fact]
    public void Reconcile_WhenAlreadyReconciled_ShouldThrow()
    {
        // Arrange - Immutability: reconciled entries cannot be reconciled again
        var entry = CreateLedgerEntry();
        var userId = Guid.NewGuid();
        entry.Reconcile(userId);

        // Act & Assert
        var act = () => entry.Reconcile(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already reconciled*");
    }

    [Fact]
    public void Reconcile_WithoutNotes_ShouldPreserveExistingNotes()
    {
        // Arrange
        var entry = CreateLedgerEntry();
        entry.Notes = "Original notes";
        var userId = Guid.NewGuid();

        // Act
        entry.Reconcile(userId);

        // Assert
        entry.Notes.Should().Be("Original notes");
    }

    #endregion

    #region Ledger Account Type Safety Tests

    [Fact]
    public void LedgerEntry_WithStronglyTypedAccounts_ShouldStoreEnumValues()
    {
        // Arrange
        var entry = new FinancialLedgerEntry
        {
            Id = Guid.NewGuid(),
            DebitLedgerAccount = LedgerAccount.CashAndBankAccounts,
            CreditLedgerAccount = LedgerAccount.RevenueSubscriptions,
            Amount = 100m,
            Currency = "USD",
            Description = "Subscription payment",
            DebitAccount = "1000",
            CreditAccount = "4000"
        };

        // Assert
        entry.DebitLedgerAccount.Should().Be(LedgerAccount.CashAndBankAccounts);
        entry.CreditLedgerAccount.Should().Be(LedgerAccount.RevenueSubscriptions);
    }

    [Fact]
    public void LedgerEntry_ShouldHaveValidDoubleEntry()
    {
        // Arrange - Double-entry bookkeeping: debit must have corresponding credit
        var entry = CreateLedgerEntry();

        // Assert
        entry.DebitAccount.Should().NotBeNullOrEmpty();
        entry.CreditAccount.Should().NotBeNullOrEmpty();
        entry.Amount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Entry Type Tests

    [Theory]
    [InlineData(LedgerEntryType.Payment)]
    [InlineData(LedgerEntryType.Refund)]
    [InlineData(LedgerEntryType.Adjustment)]
    [InlineData(LedgerEntryType.Transfer)]
    public void LedgerEntry_ShouldSupportAllEntryTypes(LedgerEntryType entryType)
    {
        // Arrange
        var entry = CreateLedgerEntry();

        // Act
        entry.EntryType = entryType;

        // Assert
        entry.EntryType.Should().Be(entryType);
    }

    #endregion

    #region Fiscal Period Tests

    [Fact]
    public void LedgerEntry_ShouldHaveFiscalYearAndPeriod()
    {
        // Arrange
        var entry = new FinancialLedgerEntry
        {
            Id = Guid.NewGuid(),
            FiscalYear = 2026,
            FiscalPeriod = 1,
            Amount = 100m,
            Currency = "USD",
            Description = "Test entry",
            DebitAccount = "1000",
            CreditAccount = "4000"
        };

        // Assert
        entry.FiscalYear.Should().Be(2026);
        entry.FiscalPeriod.Should().Be(1);
    }

    #endregion

    #region Unreconcile Removed Tests

    [Fact]
    public void LedgerEntry_ShouldNotHaveUnreconcileMethod()
    {
        // Verify that Unreconcile() method has been removed for audit trail protection
        var entryType = typeof(FinancialLedgerEntry);
        var unreconcileMethod = entryType.GetMethod("Unreconcile");

        // Assert - Method should not exist
        unreconcileMethod.Should().BeNull("Unreconcile method was removed to ensure audit trail immutability");
    }

    #endregion

    #region Helper Methods

    private static FinancialLedgerEntry CreateLedgerEntry()
    {
        return new FinancialLedgerEntry
        {
            Id = Guid.NewGuid(),
            EntryType = LedgerEntryType.Payment,
            DebitAccount = "1000",
            CreditAccount = "4000",
            DebitLedgerAccount = LedgerAccount.CashAndBankAccounts,
            CreditLedgerAccount = LedgerAccount.RevenueSubscriptions,
            Amount = 99.99m,
            Currency = "USD",
            Description = "Test payment entry",
            FiscalYear = DateTime.UtcNow.Year,
            FiscalPeriod = DateTime.UtcNow.Month
        };
    }

    #endregion
}
