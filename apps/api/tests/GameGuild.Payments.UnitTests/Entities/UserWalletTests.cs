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
        // Arrange - Critical: DeductFunds must call Touch() for concurrency protection
        var wallet = CreateActiveWallet(initialBalance: 100m);
        var initialVersion = wallet.Version;

        // Act
        wallet.DeductFunds(30m, "Test purchase");

        // Assert - Version should be incremented via Touch()
        wallet.Version.Should().BeGreaterThan(initialVersion);
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
    public void Lock_ShouldIncrementVersion()
    {
        // Arrange
        var wallet = CreateActiveWallet();
        var initialVersion = wallet.Version;

        // Act
        wallet.Lock("Test");

        // Assert
        wallet.Version.Should().BeGreaterThan(initialVersion);
    }

    [Fact]
    public void Unlock_ShouldIncrementVersion()
    {
        // Arrange
        var wallet = CreateActiveWallet();
        wallet.Lock("Test");
        var versionAfterLock = wallet.Version;

        // Act
        wallet.Unlock();

        // Assert
        wallet.Version.Should().BeGreaterThan(versionAfterLock);
    }

    #endregion

    #region Double-Spend Prevention Tests

    [Fact]
    public void DeductFunds_MultipleCallsShouldDecrementVersionEachTime()
    {
        // Arrange - Simulates that each deduction increments version for concurrency check
        var wallet = CreateActiveWallet(initialBalance: 300m);
        var version1 = wallet.Version;

        // Act
        wallet.DeductFunds(100m, "Deduction 1");
        var version2 = wallet.Version;

        wallet.DeductFunds(100m, "Deduction 2");
        var version3 = wallet.Version;

        // Assert
        version2.Should().BeGreaterThan(version1);
        version3.Should().BeGreaterThan(version2);
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
