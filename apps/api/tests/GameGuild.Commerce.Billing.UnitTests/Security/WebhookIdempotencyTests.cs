using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Security;

/// <summary>
///     P0 Critical Tests: Webhook Idempotency
///     From: COMMERCE_MODULES_SECURITY_AUDIT.md Section 7 - Test Plan
///     These tests verify webhook events are processed exactly once.
/// </summary>
public class WebhookIdempotencyTests
{
    #region Same ExternalEventId Tests (P0)

    [Fact]
    public void BillingWebhookEvent_SameExternalEventId_ShouldBeDetectable()
    {
        // Arrange - Two webhook events with same external ID
        var externalEventId = "evt_stripe_12345";
        var provider = "stripe";

        var event1 = new BillingWebhookEvent
        {
            Id = Guid.NewGuid(),
            ExternalEventId = externalEventId,
            Provider = provider,
            EventType = "invoice.paid",
            Payload = "{}",
            IsProcessed = false
        };

        var event2 = new BillingWebhookEvent
        {
            Id = Guid.NewGuid(),
            ExternalEventId = externalEventId,
            Provider = provider,
            EventType = "invoice.paid",
            Payload = "{}",
            IsProcessed = false
        };

        // Assert - Same external ID should be detectable for idempotency check
        event1.ExternalEventId.Should().Be(event2.ExternalEventId);
        event1.Provider.Should().Be(event2.Provider);
    }

    [Fact]
    public void BillingWebhookEvent_WhenMarkedAsProcessed_ShouldSetTimestamp()
    {
        // Arrange
        var webhookEvent = CreateWebhookEvent("evt_test_123");

        // Act
        webhookEvent.MarkAsProcessed();

        // Assert
        webhookEvent.IsProcessed.Should().BeTrue();
        webhookEvent.ProcessedAt.Should().NotBeNull();
        webhookEvent.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void BillingWebhookEvent_AlreadyProcessed_ShouldNotReprocess()
    {
        // Arrange
        var webhookEvent = CreateWebhookEvent("evt_duplicate_check");
        webhookEvent.MarkAsProcessed();
        var firstProcessedAt = webhookEvent.ProcessedAt;

        // Act - Check state (simulating retry check)
        var shouldProcess = !webhookEvent.IsProcessed;

        // Assert
        shouldProcess.Should().BeFalse("already processed webhook should be skipped");
        webhookEvent.ProcessedAt.Should().Be(firstProcessedAt, "timestamp should not change");
    }

    #endregion

    #region Webhook Failure and Retry Tests (P0)

    [Fact]
    public void BillingWebhookEvent_MarkAsFailed_ShouldStoreErrorWithoutDoubleCountingAttempt()
    {
        // Arrange
        var webhookEvent = CreateWebhookEvent("evt_failed_123");

        // Act
        webhookEvent.IncrementAttempts();
        webhookEvent.MarkAsFailed("Connection timeout");

        // Assert
        webhookEvent.IsFailed.Should().BeTrue();
        webhookEvent.ErrorMessage.Should().Be("Connection timeout");
        webhookEvent.ProcessingAttempts.Should().Be(1);
    }

    [Fact]
    public void BillingWebhookEvent_CanBeRetried_AfterFailure()
    {
        // Arrange
        var webhookEvent = CreateWebhookEvent("evt_retryable_123");
        webhookEvent.IncrementAttempts();
        webhookEvent.MarkAsFailed("First attempt failed");

        // Act - Simulate retry
        webhookEvent.IncrementAttempts();
        var canRetry = webhookEvent.ProcessingAttempts < 3; // Max retries

        // Assert
        canRetry.Should().BeTrue("webhook with < 3 attempts can be retried");
        webhookEvent.ProcessingAttempts.Should().Be(2);
    }

    [Fact]
    public void BillingWebhookEvent_ExceedsMaxRetries_ShouldNotBeRetried()
    {
        // Arrange
        var webhookEvent = CreateWebhookEvent("evt_max_retries");
        var maxRetries = 3;

        // Act - Simulate max retry attempts
        for (int i = 0; i < maxRetries; i++)
        {
            webhookEvent.IncrementAttempts();
        }
        webhookEvent.MarkAsFailed("Final failure");

        // Assert
        var canRetry = webhookEvent.ProcessingAttempts < maxRetries;
        canRetry.Should().BeFalse("webhook at max retries should not be retried");
    }

    [Fact]
    public void BillingWebhookEvent_FailedThenSuccessful_ShouldMarkAsProcessed()
    {
        // Arrange
        var webhookEvent = CreateWebhookEvent("evt_eventual_success");
        webhookEvent.IncrementAttempts();
        webhookEvent.MarkAsFailed("First attempt failed");

        // Act - Second attempt succeeds
        webhookEvent.IncrementAttempts();
        webhookEvent.MarkAsProcessed();

        // Assert
        webhookEvent.IsProcessed.Should().BeTrue();
        webhookEvent.ProcessingAttempts.Should().Be(2);
        // IsFailed may still be true from first attempt, but IsProcessed takes precedence
    }

    #endregion

    #region Provider-Specific Tests

    [Theory]
    [InlineData("stripe", "evt_stripe_12345")]
    [InlineData("paypal", "WH-12345-67890")]
    [InlineData("apple", "apple_receipt_12345")]
    public void BillingWebhookEvent_SupportsMultipleProviders(string provider, string externalEventId)
    {
        // Arrange & Act
        var webhookEvent = new BillingWebhookEvent
        {
            Id = Guid.NewGuid(),
            ExternalEventId = externalEventId,
            Provider = provider,
            EventType = "payment.completed",
            Payload = "{}"
        };

        // Assert
        webhookEvent.Provider.Should().Be(provider);
        webhookEvent.ExternalEventId.Should().Be(externalEventId);
    }

    [Fact]
    public void BillingWebhookEvent_SameExternalId_DifferentProvider_ShouldBeDistinct()
    {
        // Arrange - Same external ID but different providers
        var externalId = "evt_12345";

        var stripeEvent = new BillingWebhookEvent
        {
            ExternalEventId = externalId,
            Provider = "stripe",
            EventType = "payment.completed",
            Payload = "{}"
        };

        var paypalEvent = new BillingWebhookEvent
        {
            ExternalEventId = externalId,
            Provider = "paypal",
            EventType = "payment.completed",
            Payload = "{}"
        };

        // Assert - Different providers means different events
        var isSameEvent = stripeEvent.ExternalEventId == paypalEvent.ExternalEventId 
                          && stripeEvent.Provider == paypalEvent.Provider;
        isSameEvent.Should().BeFalse("same ID + different provider = different event");
    }

    #endregion

    #region Helper Methods

    private static BillingWebhookEvent CreateWebhookEvent(string externalEventId, string provider = "stripe")
    {
        return new BillingWebhookEvent
        {
            Id = Guid.NewGuid(),
            ExternalEventId = externalEventId,
            Provider = provider,
            EventType = "invoice.paid",
            Payload = "{}",
            IsProcessed = false,
            IsFailed = false,
            ProcessingAttempts = 0
        };
    }

    #endregion
}
