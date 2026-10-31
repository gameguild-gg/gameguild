using FluentAssertions;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

/// <summary>
///     Unit tests for the Stripe <c>customer.subscription.trial_will_end</c> consumer
///     (issue #408: trial ending → send reminder email).
/// </summary>
public class StripeTrialWillEndConsumerTests
{
    private readonly Mock<ISubscriptionQueryService> _subscriptionQueryService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();

    private readonly StripeTrialWillEndConsumer _consumer;

    public StripeTrialWillEndConsumerTests()
    {
        _consumer = new StripeTrialWillEndConsumer(
            _subscriptionQueryService.Object,
            _userRepository.Object,
            _notificationService.Object,
            NullLogger<StripeTrialWillEndConsumer>.Instance);
    }

    [Fact]
    public async Task TryConsumeAsync_Should_Ignore_Other_Event_Types()
    {
        var consumed = await _consumer.TryConsumeAsync(Event("customer.subscription.updated"), CancellationToken.None);

        consumed.Should().BeFalse();
        _notificationService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryConsumeAsync_Should_Queue_The_Reminder_Email_For_The_Subscription_Owner()
    {
        var (subscription, user) = ArrangeSubscriptionAndRecipient(trialEndInDays: 3);

        var consumed = await _consumer.TryConsumeAsync(
            Event("customer.subscription.trial_will_end", trialEndUnix: DateTimeOffset.UtcNow.AddDays(3).ToUnixTimeSeconds()),
            CancellationToken.None);

        consumed.Should().BeTrue();
        _notificationService.Verify(service => service.SendAsync(
            user.Id,
            NotificationType.Billing,
            It.Is<string>(title => title.Contains("trial", StringComparison.OrdinalIgnoreCase)),
            It.Is<string>(message => message.Contains("subscription trial", StringComparison.Ordinal)),
            NotificationChannel.Email,
            ((ISubscription)subscription).TenantId,
            It.IsAny<string?>(),
            GameGuild.Notifications.NotificationPriority.Normal,
            subscription.Id,
            nameof(Subscription),
            It.Is<string>(metadata => metadata.Contains(subscription.Id.ToString(), StringComparison.Ordinal)
                                       && metadata.Contains(user.Email, StringComparison.Ordinal)),
            user.Email,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryConsumeAsync_Should_Consume_Gracefully_When_The_Subscription_Is_Unknown()
    {
        _subscriptionQueryService
            .Setup(service => service.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var consumed = await _consumer.TryConsumeAsync(Event("customer.subscription.trial_will_end"), CancellationToken.None);

        consumed.Should().BeTrue("an unknown subscription is a no-op, not a retryable failure");
        _notificationService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryConsumeAsync_Should_Skip_The_Reminder_When_The_Recipient_Cannot_Receive_Mail()
    {
        var (subscription, _) = ArrangeSubscriptionAndRecipient(trialEndInDays: 3, suspended: true);

        var consumed = await _consumer.TryConsumeAsync(Event("customer.subscription.trial_will_end"), CancellationToken.None);

        consumed.Should().BeTrue();
        _notificationService.VerifyNoOtherCalls();
    }

    [Fact]
    public void ParseTrialEndUtc_Should_Read_The_Provider_Trial_End_Timestamp()
    {
        var trialEnd = DateTimeOffset.UtcNow.AddDays(3);
        var payload = "{\"data\":{\"object\":{\"id\":\"sub_1\",\"object\":\"subscription\",\"trial_end\":" + trialEnd.ToUnixTimeSeconds() + "}}}";

        var parsed = StripeTrialWillEndConsumer.ParseTrialEndUtc(payload);

        // Stripe's trial_end is a Unix-seconds timestamp, so the parsed instant carries
        // whole-second precision: compare against the same value round-tripped through
        // Unix seconds instead of the sub-second-precise source instant.
        parsed.Should().Be(DateTimeOffset.FromUnixTimeSeconds(trialEnd.ToUnixTimeSeconds()).UtcDateTime);
    }

    [Theory]
    [InlineData("""{"data":{"object":{"id":"sub_1","object":"subscription"}}}""")]
    [InlineData("not json at all")]
    [InlineData("""{"data":{"object":{"trial_end":"soon"}}}""")]
    public void ParseTrialEndUtc_Should_Return_Null_For_Missing_Or_Malformed_Values(string payload)
    {
        StripeTrialWillEndConsumer.ParseTrialEndUtc(payload).Should().BeNull();
    }

    private (Subscription Subscription, User User) ArrangeSubscriptionAndRecipient(int trialEndInDays, bool suspended = false)
    {
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(29.99m, "USD"),
            startDate: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            trialEndDate: SystemClock.UtcNow.AddDays(trialEndInDays));

        var user = User.CreateOAuthUser("subscriber@example.test", "Subscriber");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, subscription.CreatedByUserId);
        if (suspended)
        {
            user.IsSuspended = true;
        }

        _subscriptionQueryService
            .Setup(service => service.GetByExternalIdAsync("sub_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _userRepository
            .Setup(repository => repository.GetByIdAsync(subscription.CreatedByUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _notificationService
            .Setup(service => service.SendAsync(
                It.IsAny<Guid?>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationChannel>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<GameGuild.Notifications.NotificationPriority>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GameGuild.Result.Success<Notification>(null!));

        return (subscription, user);
    }

    private static VerifiedStripeWebhookEvent Event(string eventType, long? trialEndUnix = null)
    {
        var trialEndFragment = trialEndUnix.HasValue
            ? $",\"trial_end\":{trialEndUnix.Value}"
            : string.Empty;
        var verifiedPayload = "{\"data\":{\"object\":{\"id\":\"sub_1\",\"object\":\"subscription\"" + trialEndFragment + "}}}";

        return new VerifiedStripeWebhookEvent
        {
            EventId = $"evt_{Guid.NewGuid():N}",
            EventType = eventType,
            ProviderEnvironment = "live",
            ProviderAccountId = "acct_test",
            WebhookEndpointId = "we_test",
            EventSchemaVersion = "2024-06-20",
            ProviderObjectId = "sub_1",
            ProviderObjectType = "subscription",
            ProviderMonetaryLeg = "subscription",
            VerifiedPayload = verifiedPayload,
            RetainedPayload = """{"minimized":true}""",
            PayloadSha256 = new string('a', 64),
            OccurredAt = DateTimeOffset.UtcNow,
            IsLiveMode = true,
            ExternalSubscriptionId = "sub_1"
        };
    }
}
