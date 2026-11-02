using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

/// <summary>
///     Tests for the TTL purge hook in the hosted webhook worker cycle (issue #408:
///     capture raw payload and all headers): the purge runs when the opt-in retention
///     policy is enabled and is a strict no-op while the policy is off.
/// </summary>
public sealed class BillingWebhookRawDeliveryPurgeTests
{
    private readonly Mock<IBillingWebhookRepository> _webhookRepository = new();
    private readonly Mock<ISender> _sender = new();
    private readonly Mock<IBillingWebhookDeadLetterService> _deadLetterService = new();

    private static BillingConfiguration Configuration(bool retentionEnabled, bool retryEnabled = false)
    {
        var configuration = new BillingConfiguration();
        configuration.Webhook.Retention.Enabled = retentionEnabled;
        configuration.Webhook.RetryWorker.Enabled = retryEnabled;
        return configuration;
    }

    private BillingWebhookRetryWorker CreateWorker(BillingConfiguration configuration) => new(
        _webhookRepository.Object,
        _sender.Object,
        _deadLetterService.Object,
        Options.Create(configuration),
        NullLogger<BillingWebhookRetryWorker>.Instance);

    [Fact]
    public async Task ProcessDueAsync_Purges_Expired_Raw_Deliveries_When_Retention_Is_Enabled()
    {
        _webhookRepository
            .Setup(repository => repository.PurgeExpiredRawDeliveriesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        SetupNoExhaustedEvents();
        SetupNoRetryCandidates();

        var requeued = await CreateWorker(Configuration(retentionEnabled: true, retryEnabled: true)).ProcessDueAsync();

        requeued.Should().Be(0);
        _webhookRepository.Verify(
            repository => repository.PurgeExpiredRawDeliveriesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessDueAsync_Never_Touches_The_Purge_When_Retention_Is_Disabled()
    {
        SetupNoExhaustedEvents();
        SetupNoRetryCandidates();

        await CreateWorker(Configuration(retentionEnabled: false, retryEnabled: true)).ProcessDueAsync();

        _webhookRepository.Verify(
            repository => repository.PurgeExpiredRawDeliveriesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "the disabled-by-default policy must not even issue the purge query");
    }

    [Fact]
    public async Task ProcessDueAsync_A_Purge_Failure_Never_Aborts_The_Cycle()
    {
        _webhookRepository
            .Setup(repository => repository.PurgeExpiredRawDeliveriesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("purge storage failure"));
        SetupNoExhaustedEvents();
        SetupNoRetryCandidates();

        var requeued = await CreateWorker(Configuration(retentionEnabled: true, retryEnabled: true)).ProcessDueAsync();

        requeued.Should().Be(0, "the purge is best-effort: the retry cycle still completes");
    }

    private void SetupNoExhaustedEvents() =>
        _webhookRepository
            .Setup(repository => repository.GetExhaustedEventsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

    private void SetupNoRetryCandidates() =>
        _webhookRepository
            .Setup(repository => repository.GetRetryCandidatesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
}
