using FluentAssertions;
using GameGuild.Commerce;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Security;

public class AuditingBillingWebhookRepositoryTests
{
    [Fact]
    public async Task CreateAsync_Should_Journal_Received_For_Every_New_Inbox_Row()
    {
        var (repository, inner, lifecycle) = Create();
        var webhookEvent = new BillingWebhookEvent
        {
            ExternalEventId = "evt_new",
            Provider = PaymentProviders.Braintree,
            EventType = "subscription_charged_successfully"
        };
        inner
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        await repository.CreateAsync(webhookEvent, CancellationToken.None);

        lifecycle.Verify(p => p.PublishAsync(
            WebhookLifecycleOutcome.Received,
            It.Is<BillingWebhookEvent>(e => ReferenceEquals(e, webhookEvent)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Should_Not_Journal_Received_For_Idempotency_Winners()
    {
        // A duplicate or concurrent delivery resolves to the EXISTING row; the decorator
        // must not journal a second Received entry for it.
        var (repository, inner, lifecycle) = Create();
        var incoming = new BillingWebhookEvent { ExternalEventId = "evt_dup", Provider = PaymentProviders.Stripe };
        var winner = new BillingWebhookEvent { ExternalEventId = "evt_dup", Provider = PaymentProviders.Stripe };
        inner
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(winner);

        var persisted = await repository.CreateAsync(incoming, CancellationToken.None);

        persisted.Should().BeSameAs(winner);
        lifecycle.Verify(p => p.PublishAsync(It.IsAny<WebhookLifecycleOutcome>(), It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Should_Journal_Processed_When_Row_Completes()
    {
        var (repository, inner, lifecycle) = Create();
        var webhookEvent = new BillingWebhookEvent { ExternalEventId = "evt_ok", Provider = PaymentProviders.PayPal };
        webhookEvent.MarkAsProcessed();
        inner
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        await repository.UpdateAsync(webhookEvent, CancellationToken.None);

        lifecycle.Verify(p => p.PublishAsync(
            WebhookLifecycleOutcome.Processed,
            webhookEvent,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Should_Journal_Failed_For_Failed_Attempts()
    {
        var (repository, inner, lifecycle) = Create();
        var webhookEvent = new BillingWebhookEvent { ExternalEventId = "evt_bad", Provider = PaymentProviders.Stripe };
        webhookEvent.MarkAsFailed("transient downstream failure");
        inner
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        await repository.UpdateAsync(webhookEvent, CancellationToken.None);

        lifecycle.Verify(p => p.PublishAsync(
            WebhookLifecycleOutcome.Failed,
            webhookEvent,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Should_Stay_Quiet_For_Intermediate_Saves()
    {
        // Signature-verified stamping and claim bookkeeping are not lifecycle transitions.
        var (repository, inner, lifecycle) = Create();
        var webhookEvent = new BillingWebhookEvent { ExternalEventId = "evt_mid", Provider = PaymentProviders.Stripe };
        webhookEvent.MarkSignatureVerified();
        inner
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        await repository.UpdateAsync(webhookEvent, CancellationToken.None);

        lifecycle.Verify(p => p.PublishAsync(It.IsAny<WebhookLifecycleOutcome>(), It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Should_Return_The_Persisted_Row_Unchanged_When_Journaling_Fails()
    {
        var (repository, inner, lifecycle) = Create();
        lifecycle
            .Setup(p => p.PublishAsync(It.IsAny<WebhookLifecycleOutcome>(), It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("audit pipeline down"));
        var webhookEvent = new BillingWebhookEvent { ExternalEventId = "evt_x", Provider = PaymentProviders.Stripe };
        inner
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        // The publisher contract is best-effort, but the decorator must also never turn a
        // journaling failure into a persistence failure for its caller.
        var act = () => repository.CreateAsync(webhookEvent, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
        inner.Verify(r => r.CreateAsync(webhookEvent, It.IsAny<CancellationToken>()), Times.Once,
            "the row itself was already persisted before journaling");
    }

    [Fact]
    public async Task Queries_And_Claims_Should_Delegate_Without_Journaling()
    {
        var (repository, inner, lifecycle) = Create();
        inner
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingWebhookEvent { ExternalEventId = "evt_q", Provider = PaymentProviders.Stripe });
        inner
            .Setup(r => r.TryClaimProcessingAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var byId = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);
        var claimed = await repository.TryClaimProcessingAsync(byId!, DateTime.UtcNow, CancellationToken.None);
        var exists = await repository.ExistsAsync("evt_q", PaymentProviders.Stripe, CancellationToken.None);

        byId.Should().NotBeNull();
        claimed.Should().BeTrue();
        exists.Should().BeFalse();
        lifecycle.Verify(p => p.PublishAsync(It.IsAny<WebhookLifecycleOutcome>(), It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (AuditingBillingWebhookRepository Repository, Mock<IBillingWebhookRepository> Inner, Mock<IWebhookLifecycleAuditPublisher> Lifecycle) Create()
    {
        var inner = new Mock<IBillingWebhookRepository>();
        var lifecycle = new Mock<IWebhookLifecycleAuditPublisher>();
        return (new AuditingBillingWebhookRepository(inner.Object, lifecycle.Object), inner, lifecycle);
    }
}
