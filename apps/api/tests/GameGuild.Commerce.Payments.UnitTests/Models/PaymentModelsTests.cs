using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Models;

public class LedgerAccountExtensionsTests
{
    [Theory]
    [InlineData(LedgerAccount.Cash, "1000")]
    [InlineData(LedgerAccount.ProductRevenue, "4000")]
    [InlineData(LedgerAccount.SalesDiscounts, "6000")]
    public void ToAccountCode_ShouldReturn4DigitPaddedCode(LedgerAccount account, string expected)
    {
        account.ToAccountCode().Should().Be(expected);
    }

    [Theory]
    [InlineData(LedgerAccount.Cash, true)]
    [InlineData(LedgerAccount.AccountsReceivable, true)]
    [InlineData(LedgerAccount.UserWalletDeposits, true)]
    [InlineData(LedgerAccount.ProductRevenue, false)]
    public void IsAsset_ShouldClassifyCorrectly(LedgerAccount account, bool expected)
    {
        account.IsAsset().Should().Be(expected);
    }

    [Theory]
    [InlineData(LedgerAccount.AccountsPayable, true)]
    [InlineData(LedgerAccount.TaxesPayable, true)]
    [InlineData(LedgerAccount.Cash, false)]
    public void IsLiability_ShouldClassifyCorrectly(LedgerAccount account, bool expected)
    {
        account.IsLiability().Should().Be(expected);
    }

    [Theory]
    [InlineData(LedgerAccount.ProductRevenue, true)]
    [InlineData(LedgerAccount.SubscriptionRevenue, true)]
    [InlineData(LedgerAccount.Cash, false)]
    public void IsRevenue_ShouldClassifyCorrectly(LedgerAccount account, bool expected)
    {
        account.IsRevenue().Should().Be(expected);
    }

    [Theory]
    [InlineData(LedgerAccount.PaymentProcessingFees, true)]
    [InlineData(LedgerAccount.BadDebtExpense, true)]
    [InlineData(LedgerAccount.Cash, false)]
    public void IsExpense_ShouldClassifyCorrectly(LedgerAccount account, bool expected)
    {
        account.IsExpense().Should().Be(expected);
    }

    [Theory]
    [InlineData(LedgerAccount.SalesDiscounts, true)]
    [InlineData(LedgerAccount.ReturnsAndAllowances, true)]
    [InlineData(LedgerAccount.Cash, false)]
    public void IsContra_ShouldClassifyCorrectly(LedgerAccount account, bool expected)
    {
        account.IsContra().Should().Be(expected);
    }
}

public class PaymentHistoryResultTests
{
    [Fact]
    public void NetAmount_ShouldSubtractRefundAndFee()
    {
        var result = new PaymentHistoryResult
        {
            PaymentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Status = PaymentStatus.Succeeded,
            PaymentMethod = "card",
            Description = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RefundedAmount = 20m,
            ProcessingFee = 5m
        };

        result.NetAmount.Should().Be(75m);
    }

    [Fact]
    public void IsCompleted_ShouldBeTrue_WhenSucceeded()
    {
        var result = new PaymentHistoryResult
        {
            PaymentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Status = PaymentStatus.Succeeded,
            PaymentMethod = "card",
            Description = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void IsCompleted_ShouldBeFalse_WhenPending()
    {
        var result = new PaymentHistoryResult
        {
            PaymentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Status = PaymentStatus.Pending,
            PaymentMethod = "card",
            Description = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        result.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void HasRefund_ShouldBeTrue_WhenRefundedAmountPositive()
    {
        var result = new PaymentHistoryResult
        {
            PaymentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Status = PaymentStatus.Succeeded,
            PaymentMethod = "card",
            Description = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RefundedAmount = 10m
        };

        result.HasRefund.Should().BeTrue();
    }
}

public class ProcessRefundResultTests
{
    [Fact]
    public void IsSuccessful_ShouldBeTrue_WhenCompleted()
    {
        var result = new ProcessRefundResult
        {
            RefundId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            RefundedAmount = 50m,
            Currency = "USD",
            Status = TransactionStatus.Completed,
            Reason = "Customer request",
            ProcessedAt = DateTime.UtcNow
        };

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void IsSuccessful_ShouldBeFalse_WhenFailed()
    {
        var result = new ProcessRefundResult
        {
            RefundId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            RefundedAmount = 50m,
            Currency = "USD",
            Status = TransactionStatus.Failed,
            Reason = "Customer request",
            ProcessedAt = DateTime.UtcNow
        };

        result.IsSuccessful.Should().BeFalse();
    }
}
