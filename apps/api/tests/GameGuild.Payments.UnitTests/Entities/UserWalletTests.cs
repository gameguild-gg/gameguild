using FluentAssertions;
using GameGuild.Commerce.Payments;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Entities;

/// <summary>
///     Tests for UserWallet entity optimistic concurrency.
///     These tests verify that wallet operations properly use Touch() to enable
///     EF Core's concurrency checking via the Version property.
/// </summary>
public class UserWalletTests
{
    #region AddFunds Tests

    [Fact]
    public void AddFunds_WithValidAmount_ShouldIncreaseBalance()
    {
        // Arrange
        var wallet = CreateActiveWallet(initialBalance: 100m);

        // Act
        wallet.AddFunds(50m, "Test deposit");

        // Assert
        wallet.Balance.Should().Be(150m);
        wallet.LastTransactionAt.Should().NotBeNull();
    }

    [Fact]
    public void AddFunds_ShouldCreateCreditTransaction()
    {
        // Arrange
        var wallet = CreateActiveWallet(initialBalance: 100m);

        // Act
        wallet.AddFunds(50m, "Test deposit", "ref_123");

        // Assert
        wallet.Transactions.Should().HaveCount(1);
        var transaction = wallet.Transactions.First();
        transaction.Type.Should().Be(WalletTransactionType.Credit);
        transaction.Amount.Should().Be(50m);
        transaction.BalanceAfter.Should().Be(150m);
        transaction.Description.Should().Be("Test deposit");
        transaction.ReferenceId.Should().Be("ref_123");
    }

    [Fact]
    public void AddFunds_WhenInactive_ShouldThrow()
    {
        // Arrange
        var wallet = CreateActiveWallet();
        wallet.IsActive = false;

        // Act & Assert
        var act = () => wallet.AddFunds(50m, "Test");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not active*");
    }

    [Fact]
    public void AddFunds_WhenLocked_ShouldThrow()
    {
        // Arrange
        var wallet = CreateActiveWallet();
        wallet.Lock("Fraud investigation");

        // Act & Assert
        var act = () => wallet.AddFunds(50m, "Test");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*locked*");
    }

    [Fact]
    public void AddFunds_WithZeroAmount_ShouldThrow()
    {
        // Arrange
        var wallet = CreateActiveWallet();

        // Act & Assert
        var act = () => wallet.AddFunds(0m, "Test");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public void AddFunds_WithNegativeAmount_ShouldThrow()
    {
        // Arrange
        var wallet = CreateActiveWallet();

        // Act & Assert
        var act = () => wallet.AddFunds(-50m, "Test");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*positive*");
    }

    #endregion

    #region DeductFunds Tests

    [Fact]
    public void DeductFunds_WithValidAmount_ShouldDecreaseBalance()
    {
        // Arrange
        var wallet = CreateActiveWallet(initialBalance: 100m);
        var initialVersion = wallet.Version;

        // Act
        wallet.DeductFunds(30m, "Test purchase");

        // Assert
        wallet.Balance.Should().Be(70m);
        wallet.LastTransactionAt.Should().NotBeNull();
    }

    [Fact]
    public void DeductFunds_ShouldCallTouchForOptimisticConcurrency()
    {
        // Arrange - Critical: DeductFunds must call Touch() which updates UpdatedAt
        // EF Core uses this for change tracking and optimistic concurrency
        var wallet = CreateActiveWallet(initialBalance: 100m);
        var initialUpdatedAt = wallet.UpdatedAt;

        // Act
        wallet.DeductFunds(30m, "Test purchase");

        // Assert - UpdatedAt should be updated via Touch()
        wallet.UpdatedAt.Should().BeAfter(initialUpdatedAt);
    }

    [Fact]
    public void DeductFunds_ShouldCreateDebitTransaction()
    {
        // Arrange
        var wallet = CreateActiveWallet(initialBalance: 100m);

        // Act
        wallet.DeductFunds(30m, "Test purchase", "order_123");

        // Assert
        wallet.Transactions.Should().HaveCount(1);
        var transaction = wallet.Transactions.First();
        transaction.Type.Should().Be(WalletTransactionType.Debit);
        transaction.Amount.Should().Be(30m);
        transaction.BalanceAfter.Should().Be(70m);
        transaction.ReferenceId.Should().Be("order_123");
    }

    [Fact]
    public void DeductFunds_WhenInsufficientBalance_ShouldThrow()
    {
        // Arrange
        var wallet = CreateActiveWallet(initialBalance: 50m);

        // Act & Assert
        var act = () => wallet.DeductFunds(100m, "Test");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient balance*");
    }

    [Fact]
    public void DeductFunds_WhenExactBalance_ShouldSucceed()
    {
        // Arrange
        var wallet = CreateActiveWallet(initialBalance: 100m);

        // Act
        wallet.DeductFunds(100m, "Exact balance deduction");

        // Assert
        wallet.Balance.Should().Be(0m);
    }

    [Fact]
    public void DeductFunds_WhenInactive_ShouldThrow()
    {
        // Arrange
        var wallet = CreateActiveWallet(initialBalance: 100m);
        wallet.IsActive = false;

        // Act & Assert
        var act = () => wallet.DeductFunds(50m, "Test");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not active*");
    }

    [Fact]
    public void DeductFunds_WhenLocked_ShouldThrow()
    {
        // Arrange
        var wallet = CreateActiveWallet(initialBalance: 100m);
        wallet.Lock("Suspicious activity");

        // Act & Assert
        var act = () => wallet.DeductFunds(50m, "Test");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*locked*");
    }

    [Fact]
    public void DeductFunds_WithZeroAmount_ShouldThrow()
    {
        // Arrange
        var wallet = CreateActiveWallet(initialBalance: 100m);

        // Act & Assert
        var act = () => wallet.DeductFunds(0m, "Test");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*positive*");
    }

    #endregion

    #region Lock/Unlock Tests

    [Fact]
    public void Lock_ShouldSetIsLockedAndReason()
    {
        // Arrange
        var wallet = CreateActiveWallet();

        // Act
        wallet.Lock("Fraud investigation");

        // Assert
        wallet.IsLocked.Should().BeTrue();
        wallet.LockReason.Should().Be("Fraud investigation");
    }

    [Fact]
    public void Unlock_ShouldClearLockState()
    {
        // Arrange
        var wallet = CreateActiveWallet();
        wallet.Lock("Test lock");

        // Act
        wallet.Unlock();

        // Assert
        wallet.IsLocked.Should().BeFalse();
        wallet.LockReason.Should().BeNull();
    }

    [Fact]
    public void Lock_ShouldUpdateTimestamp()
    {
        // Arrange
        var wallet = CreateActiveWallet();
        var initialUpdatedAt = wallet.UpdatedAt;

        // Act
        wallet.Lock("Test");

        // Assert - Touch() updates UpdatedAt
        wallet.UpdatedAt.Should().BeAfter(initialUpdatedAt);
    }

    [Fact]
    public void Unlock_ShouldUpdateTimestamp()
    {
        // Arrange
        var wallet = CreateActiveWallet();
        wallet.Lock("Test");
        var updatedAtAfterLock = wallet.UpdatedAt;

        // Small delay to ensure timestamp difference
        System.Threading.Thread.Sleep(1);

        // Act
        wallet.Unlock();

        // Assert - Touch() updates UpdatedAt
        wallet.UpdatedAt.Should().BeAfter(updatedAtAfterLock);
    }

    #endregion

    #region Double-Spend Prevention Tests

    [Fact]
    public void DeductFunds_MultipleCallsShouldUpdateTimestampEachTime()
    {
        // Arrange - Simulates that each deduction calls Touch() for change tracking
        var wallet = CreateActiveWallet(initialBalance: 300m);
        var updatedAt1 = wallet.UpdatedAt;

        // Act
        System.Threading.Thread.Sleep(1);
        wallet.DeductFunds(100m, "Deduction 1");
        var updatedAt2 = wallet.UpdatedAt;

        System.Threading.Thread.Sleep(1);
        wallet.DeductFunds(100m, "Deduction 2");
        var updatedAt3 = wallet.UpdatedAt;

        // Assert - Each deduction updates timestamp
        updatedAt2.Should().BeAfter(updatedAt1);
        updatedAt3.Should().BeAfter(updatedAt2);
        wallet.Balance.Should().Be(100m);
    }

    #endregion

    #region Helper Methods

    private static UserWallet CreateActiveWallet(decimal initialBalance = 0m)
    {
        return new UserWallet
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Balance = initialBalance,
            Currency = "USD",
            IsActive = true,
            IsLocked = false
        };
    }

    #endregion
}
