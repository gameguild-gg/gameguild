using FluentAssertions;
using GameGuild.Commerce.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

/// <summary>
///     Issue #409 remainder: the fraud signal evaluation applied at subscription creation
///     time. Block refuses the creation; Review creates it flagged with bounded evidence;
///     Allow (and no registered service) create it unflagged.
/// </summary>
public class CreateSubscriptionFraudGateTests
{
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepository = new();
    private readonly Mock<IFraudDetectionService> _fraudDetection = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _planId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public CreateSubscriptionFraudGateTests()
    {
        _planRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionPlan("Test Plan", "test-plan", 1900, "USD"));
        _subscriptionRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription subscription, CancellationToken _) => subscription);
        _subscriptionRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription subscription, CancellationToken _) => subscription);
    }

    [Fact]
    public async Task Handle_Should_Refuse_Creation_When_The_Gate_Blocks()
    {
        _fraudDetection
            .Setup(service => service.AssessSubscriptionCreationAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assessment(FraudDecision.Block, 95));

        var handler = CreateHandler();
        var act = () => handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<SubscriptionFraudBlockedException>();
        _subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "a blocked creation never persists a subscription");
    }

    [Fact]
    public async Task Handle_Should_Create_Flagged_With_Evidence_When_The_Gate_Holds_For_Review()
    {
        _fraudDetection
            .Setup(service => service.AssessSubscriptionCreationAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assessment(FraudDecision.Review, 55));

        var handler = CreateHandler();
        var subscriptionId = await handler.Handle(Command(), CancellationToken.None);

        subscriptionId.Should().NotBe(Guid.Empty);
        _subscriptionRepository.Verify(
            repository => repository.UpdateAsync(
                It.Is<Subscription>(subscription =>
                    subscription.Metadata != null
                    && subscription.Metadata.Contains("\"decision\":\"Review\"")
                    && subscription.Metadata.Contains("\"score\":55")),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "the review hold persists its bounded evidence on the subscription");
    }

    [Fact]
    public async Task Handle_Should_Create_Unflagged_When_The_Gate_Allows()
    {
        _fraudDetection
            .Setup(service => service.AssessSubscriptionCreationAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assessment(FraudDecision.Allow, 5));

        var handler = CreateHandler();
        var subscriptionId = await handler.Handle(Command(), CancellationToken.None);

        subscriptionId.Should().NotBe(Guid.Empty);
        _subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "an allowed creation needs no fraud metadata write");
    }

    [Fact]
    public async Task Handle_Should_Assess_With_The_Creation_Metadata()
    {
        FraudSignalContext? captured = null;
        _fraudDetection
            .Setup(service => service.AssessSubscriptionCreationAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .Callback<FraudSignalContext, CancellationToken>((context, _) => captured = context)
            .ReturnsAsync(Assessment(FraudDecision.Allow, 0));

        var handler = CreateHandler();
        await handler.Handle(
            Command() with
            {
                FraudCountry = "PT",
                FraudSourceIpAddress = "203.0.113.10",
                FraudDeviceFingerprint = "fp-1",
                FraudPayerEmail = "user@example.org",
                FraudCardBin = "530092",
                FraudClientTimezone = "Europe/Lisbon"
            },
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(_tenantId);
        captured.PaymentId.Should().Be(Guid.Empty, "no payment row exists at creation time");
        captured.BillingCycleNumber.Should().Be(1);
        captured.Amount.Should().Be(19m);
        captured.Country.Should().Be("PT");
        captured.SourceIpAddress.Should().Be("203.0.113.10");
        captured.DeviceFingerprint.Should().Be("fp-1");
        captured.PayerEmail.Should().Be("user@example.org");
        captured.CardBin.Should().Be("530092");
        captured.ClientTimezone.Should().Be("Europe/Lisbon");
    }

    [Fact]
    public async Task Handle_Should_Skip_The_Gate_When_No_Fraud_Service_Is_Registered()
    {
        var handler = new CreateSubscriptionCommandHandler(
            _subscriptionRepository.Object,
            _planRepository.Object,
            NullLogger<CreateSubscriptionCommandHandler>.Instance);

        var subscriptionId = await handler.Handle(Command(), CancellationToken.None);

        subscriptionId.Should().NotBe(Guid.Empty);
        _subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private CreateSubscriptionCommandHandler CreateHandler() =>
        new(
            _subscriptionRepository.Object,
            _planRepository.Object,
            NullLogger<CreateSubscriptionCommandHandler>.Instance,
            promoCoordinator: null,
            fraudDetectionService: _fraudDetection.Object);

    private CreateSubscriptionCommand Command() => new(
        _tenantId,
        _planId,
        _userId,
        BillingCycle.Monthly,
        19m);

    private static FraudAssessment Assessment(FraudDecision decision, int score) => new(
        Guid.NewGuid(),
        Guid.Empty,
        score,
        FraudAssessment.RiskLevelForScore(score),
        decision,
        $"assessment outcome {decision}",
        [],
        SystemClock.UtcNow);
}
