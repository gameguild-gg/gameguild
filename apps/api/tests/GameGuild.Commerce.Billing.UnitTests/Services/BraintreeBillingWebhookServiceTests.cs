using FluentAssertions;
using GameGuild.Commerce.Subscriptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

public class BraintreeBillingWebhookServiceTests
{
    private static BraintreeWebhookNotification ChargedNotification() => new()
    {
        Kind = "subscription_charged_successfully",
        TimestampUtc = new DateTime(2026, 10, 31, 9, 30, 0, DateTimeKind.Utc),
        SubscriptionId = "sub_7j8k9l",
        SubscriptionStatus = "active",
        TransactionId = "txn_5f6g7h",
        TransactionStatus = "submitted_for_settlement",
        Amount = 29.00m,
        CurrencyIsoCode = "USD"
    };

    [Fact]
    public async Task ProcessBraintreeWebhookAsync_Should_Reject_Failed_Verification_Without_Inbox_Row()
    {
        // Braintree verifies FIRST (issue #408): nothing about an unauthenticated delivery
        // may touch the inbox — there is no trustworthy identity before the signature check.
        var repository = new Mock<IBillingWebhookRepository>();
        var verification = new Mock<IBraintreeWebhookVerificationService>();
        verification
            .Setup(v => v.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(BraintreeWebhookVerificationResult.Failed("Signature digest mismatch"));

        var service = CreateService(repository, verification.Object);

        var result = await service.ProcessBraintreeWebhookAsync("key|bad", "cGF5bG9hZA==", CancellationToken.None);

        result.Processed.Should().BeFalse();
        result.RequiresRetry.Should().BeFalse("a failed signature is never retried");
        repository.Verify(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.GetByExternalEventIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBraintreeWebhookAsync_Should_Return_AlreadyProcessed_For_Duplicate()
    {
        var notification = ChargedNotification();
        var eventId = BraintreeBillingWebhookService.DeriveEventId(notification);
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync(eventId, PaymentProviders.Braintree, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingWebhookEvent
            {
                ExternalEventId = eventId,
                Provider = PaymentProviders.Braintree,
                IsProcessed = true,
                ProcessedAt = DateTime.UtcNow
            });

        var service = CreateService(repository, Verified(notification));

        var result = await service.ProcessBraintreeWebhookAsync("sig", "payload", CancellationToken.None);

        result.WasAlreadyProcessed.Should().BeTrue();
        repository.Verify(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never,
            "a completed duplicate is acknowledged without reprocessing");
    }

    [Fact]
    public async Task ProcessBraintreeWebhookAsync_Should_Process_Verified_Delivery_And_Mark_Row()
    {
        var notification = ChargedNotification();
        var eventId = BraintreeBillingWebhookService.DeriveEventId(notification);
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync(eventId, PaymentProviders.Braintree, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        BillingWebhookEvent? stored = null;
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .Callback<BillingWebhookEvent, CancellationToken>((e, _) => stored = e)
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var service = CreateService(repository, Verified(notification));

        var result = await service.ProcessBraintreeWebhookAsync("sig", "payload", CancellationToken.None);

        result.Processed.Should().BeTrue();
        result.EventId.Should().Be(eventId);
        stored.Should().NotBeNull();
        stored!.Provider.Should().Be(PaymentProviders.Braintree);
        stored.EventType.Should().Be("subscription_charged_successfully");
        stored.IsSignatureVerified.Should().BeTrue("the delivery was authenticated before the row existed");
        stored.IsProcessed.Should().BeTrue();
        stored.Payload.Should().StartWith("{").And.Contain("subscription_charged_successfully",
            "the stored payload is the JSON routing projection, never the raw provider body");
    }

    [Fact]
    public async Task ProcessBraintreeWebhookAsync_Should_Reprocess_PreviouslyFailed_Redelivery()
    {
        var notification = ChargedNotification();
        var eventId = BraintreeBillingWebhookService.DeriveEventId(notification);
        var failedRow = new BillingWebhookEvent { ExternalEventId = eventId, Provider = PaymentProviders.Braintree, ProcessingAttempts = 1 };
        failedRow.MarkAsFailed("transient downstream failure");

        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync(eventId, PaymentProviders.Braintree, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedRow);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var service = CreateService(repository, Verified(notification));

        var result = await service.ProcessBraintreeWebhookAsync("sig", "payload", CancellationToken.None);

        result.Processed.Should().BeTrue();
        result.WasAlreadyProcessed.Should().BeFalse();
        failedRow.IsProcessed.Should().BeTrue();
        failedRow.ProcessingAttempts.Should().Be(2, "the redelivery reuses the existing row as its next attempt");
        repository.Verify(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBraintreeWebhookAsync_Should_Handle_Unknown_Kind_Gracefully()
    {
        var notification = new BraintreeWebhookNotification { Kind = "account_updated" };
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync(It.IsAny<string>(), PaymentProviders.Braintree, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var service = CreateService(repository, Verified(notification));

        var result = await service.ProcessBraintreeWebhookAsync("sig", "payload", CancellationToken.None);

        result.Processed.Should().BeTrue("unknown event kinds are acknowledged for audit without failing delivery");
    }

    [Fact]
    public void DeriveEventId_Should_Be_Deterministic_Across_Identical_Deliveries()
    {
        var notification = ChargedNotification();

        BraintreeBillingWebhookService.DeriveEventId(notification)
            .Should().Be(BraintreeBillingWebhookService.DeriveEventId(ChargedNotification()));
    }

    [Fact]
    public void MinimizePayload_RoundTrips_Through_ParseMinimizedPayload()
    {
        var notification = ChargedNotification();

        var minimized = BraintreeBillingWebhookService.MinimizePayload(notification);
        var restored = BraintreeBillingWebhookService.ParseMinimizedPayload(minimized);

        restored.Should().NotBeNull();
        restored!.Kind.Should().Be(notification.Kind);
        restored.SubscriptionId.Should().Be(notification.SubscriptionId);
        restored.TransactionId.Should().Be(notification.TransactionId);
        restored.Amount.Should().Be(notification.Amount);
        restored.CurrencyIsoCode.Should().Be(notification.CurrencyIsoCode);
    }

    [Fact]
    public async Task ReplayBraintreeWebhookAsync_Should_Refuse_Unverified_Rows()
    {
        var unverifiedRow = new BillingWebhookEvent
        {
            ExternalEventId = "evt",
            Provider = PaymentProviders.Braintree,
            EventType = "subscription_charged_successfully",
            Payload = "{}"
        };
        unverifiedRow.MarkAsFailed("imported without verification");

        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var service = CreateService(repository, Mock.Of<IBraintreeWebhookVerificationService>());

        var result = await service.ReplayBraintreeWebhookAsync(unverifiedRow, CancellationToken.None);

        result.Processed.Should().BeFalse();
        result.RequiresRetry.Should().BeFalse();
        unverifiedRow.IsProcessed.Should().BeFalse();
    }

    [Fact]
    public async Task ReplayBraintreeWebhookAsync_Should_Route_Verified_Row_From_Stored_Projection()
    {
        var notification = ChargedNotification();
        var row = new BillingWebhookEvent
        {
            ExternalEventId = BraintreeBillingWebhookService.DeriveEventId(notification),
            Provider = PaymentProviders.Braintree,
            EventType = notification.Kind,
            Payload = BraintreeBillingWebhookService.MinimizePayload(notification)
        };
        row.MarkSignatureVerified();

        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var service = CreateService(repository, Mock.Of<IBraintreeWebhookVerificationService>());

        var result = await service.ReplayBraintreeWebhookAsync(row, CancellationToken.None);

        result.Processed.Should().BeTrue();
        row.IsProcessed.Should().BeTrue();
    }

    private static IBraintreeWebhookVerificationService Verified(BraintreeWebhookNotification notification)
    {
        var verification = new Mock<IBraintreeWebhookVerificationService>();
        verification
            .Setup(v => v.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(BraintreeWebhookVerificationResult.Success(notification));
        return verification.Object;
    }

    private static BraintreeBillingWebhookService CreateService(
        Mock<IBillingWebhookRepository> repository,
        IBraintreeWebhookVerificationService verification)
    {
        return new BraintreeBillingWebhookService(
            repository.Object,
            verification,
            NullLogger<BraintreeBillingWebhookService>.Instance,
            Mock.Of<ISubscriptionLifecycleService>(),
            Mock.Of<ISubscriptionQueryService>(),
            Mock.Of<ISubscriptionBillingService>(),
            Mock.Of<ISubscriptionExternalIdService>());
    }
}
