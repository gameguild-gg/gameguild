using FluentAssertions;
using GameGuild.Commerce.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

/// <summary>
///     Issue #409 remainder: subscription CHANGES run the fraud signal evaluation. A
///     Block-level assessment refuses the upgrade/downgrade/reactivation before any
///     mutation; Review persists bounded evidence (when the metadata slot is free) and
///     opens a review ticket; Allow (and no registered service) mutate unflagged.
/// </summary>
public class SubscriptionChangeFraudGateTests
{
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<ISubscriptionLifecycleService> _lifecycleService = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepository = new();
    private readonly Mock<IFraudDetectionService> _fraudDetection = new();
    private readonly Mock<GameGuild.Commerce.IFraudReviewTicketCoordinator> _ticketCoordinator = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _planId = Guid.NewGuid();
    private readonly Guid _newPlanId = Guid.NewGuid();
    private readonly Guid _subscriptionId = Guid.NewGuid();

    public SubscriptionChangeFraudGateTests()
    {
        _planRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionPlan("Test Plan", "test-plan", 1900, "USD"));
        _subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Subscription());
        _subscriptionRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription subscription, CancellationToken _) => subscription);
        _lifecycleService
            .Setup(service => service.UpgradePlanAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionUpgradeResult { Success = true, UpdatedSubscription = Subscription() });
        _lifecycleService
            .Setup(service => service.DowngradePlanAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionDowngradeResult { Success = true, UpdatedSubscription = Subscription() });
    }

    [Fact]
    public async Task Upgrade_Should_Be_Refused_When_The_Gate_Blocks()
    {
        _fraudDetection
            .Setup(service => service.AssessSubscriptionChangeAsync(
                It.IsAny<FraudSignalContext>(),
                SubscriptionFraudChangeKind.Upgrade,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assessment(FraudDecision.Block, 95));

        var act = () => CreateUpgradeHandler().Handle(UpgradeCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<SubscriptionFraudBlockedException>();
        _lifecycleService.Verify(
            service => service.UpgradePlanAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "a blocked upgrade never mutates the subscription");
    }

    [Fact]
    public async Task Downgrade_Should_Be_Refused_When_The_Gate_Blocks()
    {
        _fraudDetection
            .Setup(service => service.AssessSubscriptionChangeAsync(
                It.IsAny<FraudSignalContext>(),
                SubscriptionFraudChangeKind.Downgrade,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assessment(FraudDecision.Block, 90));

        var act = () => CreateDowngradeHandler().Handle(DowngradeCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<SubscriptionFraudBlockedException>();
        _lifecycleService.Verify(
            service => service.DowngradePlanAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "a blocked downgrade never mutates the subscription");
    }

    [Fact]
    public async Task Reactivation_Should_Be_Refused_When_The_Gate_Blocks()
    {
        _fraudDetection
            .Setup(service => service.AssessSubscriptionChangeAsync(
                It.IsAny<FraudSignalContext>(),
                SubscriptionFraudChangeKind.Reactivation,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assessment(FraudDecision.Block, 88));

        var act = () => CreateReactivateHandler().Handle(ReactivateCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<SubscriptionFraudBlockedException>();
        _lifecycleService.Verify(
            service => service.ReactivateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "a blocked reactivation never resumes billing");
    }

    [Fact]
    public async Task Upgrade_Should_Persist_Evidence_And_Open_A_Ticket_When_Held_For_Review()
    {
        Subscription? persisted = null;
        _subscriptionRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<Subscription, CancellationToken>((subscription, _) => persisted = subscription)
            .ReturnsAsync((Subscription subscription, CancellationToken _) => subscription);
        _fraudDetection
            .Setup(service => service.AssessSubscriptionChangeAsync(
                It.IsAny<FraudSignalContext>(),
                SubscriptionFraudChangeKind.Upgrade,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assessment(FraudDecision.Review, 55));

        await CreateUpgradeHandler().Handle(UpgradeCommand(), CancellationToken.None);

        persisted!.Metadata.Should().Contain("\"decision\":\"Review\"").And.Contain("\"score\":55");
        _ticketCoordinator.Verify(
            coordinator => coordinator.OpenReviewTicketAsync(
                It.Is<GameGuild.Commerce.FraudReviewTicketRequest>(request =>
                    request.SubjectEntityId == _subscriptionId
                    && request.SubjectEntityType == "Subscription"
                    && request.Score == 55),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "a review-held change opens a support ticket through the commerce coordinator");
        _lifecycleService.Verify(
            service => service.UpgradePlanAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "a review-held upgrade still applies the change — the charge-time gate holds the money");
    }

    [Fact]
    public async Task Reactivation_Should_Assess_With_The_Attempt_Metadata()
    {
        FraudSignalContext? captured = null;
        _fraudDetection
            .Setup(service => service.AssessSubscriptionChangeAsync(
                It.IsAny<FraudSignalContext>(),
                SubscriptionFraudChangeKind.Reactivation,
                It.IsAny<CancellationToken>()))
            .Callback<FraudSignalContext, SubscriptionFraudChangeKind, CancellationToken>((context, _, _) => captured = context)
            .ReturnsAsync(Assessment(FraudDecision.Allow, 0));

        await CreateReactivateHandler().Handle(
            ReactivateCommand() with
            {
                FraudCountry = "PT",
                FraudSourceIpAddress = "203.0.113.10",
                FraudDeviceFingerprint = "fp-reactivation",
                FraudPayerEmail = "user@example.org",
                FraudCardBin = "530092",
                FraudClientTimezone = "Europe/Lisbon"
            },
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(_tenantId);
        captured.SubscriptionId.Should().Be(_subscriptionId);
        captured.PaymentId.Should().Be(Guid.Empty, "no payment row exists at change time");
        captured.Amount.Should().Be(19m);
        captured.Country.Should().Be("PT");
        captured.SourceIpAddress.Should().Be("203.0.113.10");
        captured.DeviceFingerprint.Should().Be("fp-reactivation");
        captured.PayerEmail.Should().Be("user@example.org");
        captured.CardBin.Should().Be("530092");
        captured.ClientTimezone.Should().Be("Europe/Lisbon");
        _subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "an allowed change needs no fraud metadata write");
    }

    [Fact]
    public async Task Upgrade_Should_Mutate_Unflagged_When_No_Service_Is_Registered()
    {
        var handler = new UpgradeSubscriptionPlanCommandHandler(
            _lifecycleService.Object,
            _planRepository.Object,
            _subscriptionRepository.Object,
            NullLogger<UpgradeSubscriptionPlanCommandHandler>.Instance);

        await handler.Handle(UpgradeCommand(), CancellationToken.None);

        _lifecycleService.Verify(
            service => service.UpgradePlanAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "without a fraud service the change proceeds like before (the payment gate still evaluates charges)");
    }

    private UpgradeSubscriptionPlanCommandHandler CreateUpgradeHandler() =>
        new(
            _lifecycleService.Object,
            _planRepository.Object,
            _subscriptionRepository.Object,
            NullLogger<UpgradeSubscriptionPlanCommandHandler>.Instance,
            promoCoordinator: null,
            fraudDetectionService: _fraudDetection.Object,
            reviewTicketCoordinator: _ticketCoordinator.Object);

    private DowngradeSubscriptionPlanCommandHandler CreateDowngradeHandler() =>
        new(
            _lifecycleService.Object,
            _planRepository.Object,
            _subscriptionRepository.Object,
            NullLogger<DowngradeSubscriptionPlanCommandHandler>.Instance,
            promoCoordinator: null,
            fraudDetectionService: _fraudDetection.Object,
            reviewTicketCoordinator: _ticketCoordinator.Object);

    private ReactivateSubscriptionCommandHandler CreateReactivateHandler() =>
        new(
            _lifecycleService.Object,
            _subscriptionRepository.Object,
            NullLogger<ReactivateSubscriptionCommandHandler>.Instance,
            fraudDetectionService: _fraudDetection.Object,
            reviewTicketCoordinator: _ticketCoordinator.Object);

    private UpgradeSubscriptionPlanCommand UpgradeCommand() =>
        new(_subscriptionId, _newPlanId, EffectiveDate: null, PromoCodes: null, PromoCodeIdempotencyKey: null,
            FraudCountry: null, FraudSourceIpAddress: null, FraudDeviceFingerprint: null,
            FraudPayerEmail: null, FraudCardBin: null, FraudClientTimezone: null);

    private DowngradeSubscriptionPlanCommand DowngradeCommand() =>
        new(_subscriptionId, _newPlanId, EffectiveDate: null, PromoCodes: null, PromoCodeIdempotencyKey: null,
            FraudCountry: null, FraudSourceIpAddress: null, FraudDeviceFingerprint: null,
            FraudPayerEmail: null, FraudCardBin: null, FraudClientTimezone: null);

    private ReactivateSubscriptionCommand ReactivateCommand() => new(_subscriptionId);

    private Subscription Subscription() =>
        new(_tenantId, _planId, _userId, BillingCycle.Monthly, new Money(19m), SystemClock.UtcNow);

    private static FraudAssessment Assessment(FraudDecision decision, int score) =>
        new(
            Guid.NewGuid(),
            Guid.Empty,
            score,
            FraudAssessment.RiskLevelForScore(score),
            decision,
            $"unit-test {decision}",
            [],
            SystemClock.UtcNow);
}
