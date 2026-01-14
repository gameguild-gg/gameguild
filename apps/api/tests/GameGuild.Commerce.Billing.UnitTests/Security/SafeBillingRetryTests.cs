using FluentAssertions;
using GameGuild.Commerce.Billing;
using GameGuild.Commerce.Subscriptions;
using GameGuild.ValueObjects;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Security;

/// <summary>
///     P0/P1 Critical Tests: Safe Billing Retries
///     From: COMMERCE_MODULES_SECURITY_AUDIT.md Section 7 - Test Plan
///     These tests verify that failed payments are retried safely with proper limits.
/// </summary>
public class SafeBillingRetryTests
{
    #region Payment Retry Limit Tests (P0)

    [Fact]
    public void PaymentAttempt_TrackFailedAttempts()
    {
        // Arrange
        var attempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            Amount = 29.99m,
            Currency = "USD",
            FailedAttempts = 0,
            MaxRetries = 3
        };

        // Act
        attempt.RecordFailure("Card declined");

        // Assert
        attempt.FailedAttempts.Should().Be(1);
        attempt.LastError.Should().Be("Card declined");
    }

    [Fact]
    public void PaymentAttempt_ReachesMaxRetries_ShouldStopRetrying()
    {
        // Arrange
        var attempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            Amount = 29.99m,
            Currency = "USD",
            FailedAttempts = 0,
            MaxRetries = 3
        };

        // Act - Simulate 3 failures
        attempt.RecordFailure("Card declined");
        attempt.RecordFailure("Card declined");
        attempt.RecordFailure("Card declined");

        // Assert
        attempt.FailedAttempts.Should().Be(3);
        attempt.CanRetry().Should().BeFalse("max retries reached");
    }

    [Fact]
    public void PaymentAttempt_BelowMaxRetries_CanRetry()
    {
        // Arrange
        var attempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            Amount = 29.99m,
            Currency = "USD",
            FailedAttempts = 1,
            MaxRetries = 3
        };

        // Assert
        attempt.CanRetry().Should().BeTrue("below max retries");
    }

    [Fact]
    public void PaymentAttempt_MaxRetriesIs3ByDefault()
    {
        // Arrange & Act
        var attempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            Amount = 29.99m,
            Currency = "USD"
        };

        // Assert
        attempt.MaxRetries.Should().Be(3);
    }

    #endregion

    #region Subscription PastDue Status Tests (P0)

    [Fact]
    public void Subscription_AfterMaxRetries_TransitionsToPastDue()
    {
        // Arrange
        var subscription = CreateActiveSubscription();

        // Act
        subscription.MarkPaymentFailed();
        subscription.MarkPaymentFailed();
        subscription.MarkPaymentFailed(); // Third failure

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.PastDue);
        subscription.FailedPaymentCount.Should().Be(3);
    }

    [Fact]
    public void Subscription_PastDue_DisablesAccess()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.TransitionTo(SubscriptionStatus.PastDue);

        // Assert
        subscription.IsAccessible.Should().BeFalse("PastDue subscriptions should not grant access");
    }

    [Fact]
    public void Subscription_SingleFailure_RemainActive()
    {
        // Arrange
        var subscription = CreateActiveSubscription();

        // Act
        subscription.MarkPaymentFailed();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.FailedPaymentCount.Should().Be(1);
    }

    [Fact]
    public void Subscription_PaymentSuccess_ResetsFailureCount()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.MarkPaymentFailed();
        subscription.MarkPaymentFailed();

        // Act
        subscription.RecordPaymentSuccess();

        // Assert
        subscription.FailedPaymentCount.Should().Be(0);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    #endregion

    #region Invoice Retry Tracking Tests (P0)

    [Fact]
    public void Invoice_TracksPaymentAttempts()
    {
        // Arrange
        var invoice = CreateInvoice();

        // Act
        invoice.RecordPaymentAttempt(success: false, error: "Card declined");
        invoice.RecordPaymentAttempt(success: false, error: "Insufficient funds");

        // Assert
        invoice.PaymentAttemptCount.Should().Be(2);
        invoice.LastPaymentError.Should().Be("Insufficient funds");
    }

    [Fact]
    public void Invoice_SuccessfulPayment_MarksAsPaid()
    {
        // Arrange
        var invoice = CreateInvoice();
        invoice.RecordPaymentAttempt(success: false, error: "First attempt failed");

        // Act
        invoice.RecordPaymentAttempt(success: true, transactionId: "txn_123");

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAt.Should().NotBeNull();
        invoice.TransactionId.Should().Be("txn_123");
    }

    [Fact]
    public void Invoice_MaxRetriesExceeded_MarksAsFailed()
    {
        // Arrange
        var invoice = CreateInvoice();
        invoice.MaxPaymentAttempts = 3;

        // Act - Simulate 3 failures
        invoice.RecordPaymentAttempt(success: false, error: "Attempt 1");
        invoice.RecordPaymentAttempt(success: false, error: "Attempt 2");
        invoice.RecordPaymentAttempt(success: false, error: "Attempt 3");

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Failed);
        invoice.CanRetryPayment.Should().BeFalse();
    }

    #endregion

    #region Retry Delay/Backoff Tests (P1)

    [Fact]
    public void PaymentAttempt_CalculatesExponentialBackoff()
    {
        // Arrange
        var attempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            FailedAttempts = 0,
            BaseRetryDelayMinutes = 60 // 1 hour base
        };

        // Act & Assert - Exponential backoff: 1h, 2h, 4h
        attempt.RecordFailure("Failure 1");
        attempt.GetNextRetryDelay().Should().Be(TimeSpan.FromMinutes(60));

        attempt.RecordFailure("Failure 2");
        attempt.GetNextRetryDelay().Should().Be(TimeSpan.FromMinutes(120));

        attempt.RecordFailure("Failure 3");
        attempt.GetNextRetryDelay().Should().Be(TimeSpan.FromMinutes(240));
    }

    [Fact]
    public void PaymentAttempt_BackoffHasMaximumLimit()
    {
        // Arrange
        var attempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            FailedAttempts = 10, // Many failures
            BaseRetryDelayMinutes = 60,
            MaxRetryDelayMinutes = 1440 // 24 hours max
        };

        // Assert - Should not exceed max delay
        var delay = attempt.GetNextRetryDelay();
        delay.Should().BeLessOrEqualTo(TimeSpan.FromMinutes(1440));
    }

    #endregion

    #region Grace Period Tests (P1)

    [Fact]
    public void Subscription_PastDue_HasGracePeriod()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.GracePeriodDays = 7;

        // Act
        subscription.TransitionTo(SubscriptionStatus.PastDue);

        // Assert
        subscription.GracePeriodEndsAt.Should().BeCloseTo(
            DateTime.UtcNow.AddDays(7),
            TimeSpan.FromSeconds(5)
        );
    }

    [Fact]
    public void Subscription_WithinGracePeriod_StillHasLimitedAccess()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.GracePeriodDays = 7;
        subscription.TransitionTo(SubscriptionStatus.PastDue);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.PastDue);
        subscription.IsInGracePeriod.Should().BeTrue();
        subscription.HasGracePeriodAccess.Should().BeTrue();
    }

    [Fact]
    public void Subscription_AfterGracePeriod_NoAccess()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.GracePeriodDays = 7;
        subscription.TransitionTo(SubscriptionStatus.PastDue);

        // Simulate grace period expired
        subscription.SetGracePeriodExpired();

        // Assert
        subscription.IsInGracePeriod.Should().BeFalse();
        subscription.HasGracePeriodAccess.Should().BeFalse();
        subscription.IsAccessible.Should().BeFalse();
    }

    #endregion

    #region Dunning Notification Tests (P1)

    [Fact]
    public void Subscription_PaymentFailed_ShouldTriggerNotification()
    {
        // Arrange
        var subscription = CreateActiveSubscription();

        // Act
        subscription.MarkPaymentFailed();

        // Assert
        subscription.PendingNotifications.Should().Contain(n => n.Type == NotificationType.PaymentFailed);
    }

    [Fact]
    public void Subscription_PastDue_ShouldTriggerUrgentNotification()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.TransitionTo(SubscriptionStatus.PastDue);

        // Assert
        subscription.PendingNotifications.Should().Contain(n => 
            n.Type == NotificationType.SubscriptionPastDue && n.IsUrgent);
    }

    #endregion

    #region Helper Methods

    private static Subscription CreateActiveSubscription()
    {
        return new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(29.99m, "USD"),
            startDate: DateTime.UtcNow
        )
        {
            Status = SubscriptionStatus.Active,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            AutoRenew = true,
            FailedPaymentCount = 0
        };
    }

    private static Invoice CreateInvoice()
    {
        return Invoice.Create(
            subscriptionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            amount: new Money(29.99m, "USD"),
            periodStart: DateTime.UtcNow,
            periodEnd: DateTime.UtcNow.AddMonths(1)
        );
    }

    #endregion
}

/// <summary>
/// Supporting types for SafeBillingRetryTests
/// These would typically be in the domain layer
/// </summary>
public class PaymentAttempt
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public int FailedAttempts { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string? LastError { get; set; }
    public int BaseRetryDelayMinutes { get; set; } = 60;
    public int MaxRetryDelayMinutes { get; set; } = 1440;

    public void RecordFailure(string error)
    {
        FailedAttempts++;
        LastError = error;
    }

    public bool CanRetry() => FailedAttempts < MaxRetries;

    public TimeSpan GetNextRetryDelay()
    {
        var delayMinutes = BaseRetryDelayMinutes * Math.Pow(2, FailedAttempts - 1);
        delayMinutes = Math.Min(delayMinutes, MaxRetryDelayMinutes);
        return TimeSpan.FromMinutes(delayMinutes);
    }
}

public enum NotificationType
{
    PaymentFailed,
    SubscriptionPastDue,
    GracePeriodEnding,
    SubscriptionCancelled
}

public class PendingNotification
{
    public NotificationType Type { get; set; }
    public bool IsUrgent { get; set; }
}
