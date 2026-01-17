using FluentAssertions;
using GameGuild.Entities;
using GameGuild.SharedKernel;
using GameGuild.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Services;

/// <summary>
///     Comprehensive unit tests for SubscriptionService covering:
///     - ISubscriptionLifecycleService operations
///     - ISubscriptionBillingService operations
///     - ISubscriptionQueryService operations
///     - ISubscriptionExternalIdService operations
/// </summary>
public class SubscriptionServiceTests
{
    private readonly Mock<ISubscriptionRepository> _mockRepository;
    private readonly Mock<ISubscriptionPlanService> _mockPlanService;
    private readonly Mock<ISubscriptionNotificationService> _mockNotificationService;
    private readonly Mock<ILogger<SubscriptionService>> _mockLogger;
    private readonly SubscriptionService _service;

    public SubscriptionServiceTests()
    {
        _mockRepository = new Mock<ISubscriptionRepository>();
        _mockPlanService = new Mock<ISubscriptionPlanService>();
        _mockNotificationService = new Mock<ISubscriptionNotificationService>();
        _mockLogger = new Mock<ILogger<SubscriptionService>>();

        _service = new SubscriptionService(
            _mockRepository.Object,
            _mockPlanService.Object,
            _mockNotificationService.Object,
            _mockLogger.Object
        );
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrow_WhenRepositoryIsNull()
    {
        // Act
        var act = () => new SubscriptionService(
            null!,
            _mockPlanService.Object,
            _mockNotificationService.Object,
            _mockLogger.Object
        );

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenPlanServiceIsNull()
    {
        // Act
        var act = () => new SubscriptionService(
            _mockRepository.Object,
            null!,
            _mockNotificationService.Object,
            _mockLogger.Object
        );

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("planService");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenNotificationServiceIsNull()
    {
        // Act
        var act = () => new SubscriptionService(
            _mockRepository.Object,
            _mockPlanService.Object,
            null!,
            _mockLogger.Object
        );

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("notificationService");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        // Act
        var act = () => new SubscriptionService(
            _mockRepository.Object,
            _mockPlanService.Object,
            _mockNotificationService.Object,
            null!
        );

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldCreateInstance_WhenAllDependenciesProvided()
    {
        // Act
        var service = new SubscriptionService(
            _mockRepository.Object,
            _mockPlanService.Object,
            _mockNotificationService.Object,
            _mockLogger.Object
        );

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region ISubscriptionLifecycleService Tests

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ShouldCreateSubscription_WhenValidPlanExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var billingCycle = BillingCycle.Monthly;
        var amount = new Money(29.99m, "USD");
        var plan = CreateTestPlan(planId);

        _mockPlanService.Setup(p => p.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.CreateAsync(tenantId, planId, userId, billingCycle, amount);

        // Assert
        result.Should().NotBeNull();
        result.TenantId.Value.Should().Be(tenantId);
        result.PlanId.Should().Be(planId);
        result.CreatedByUserId.Should().Be(userId);
        result.BillingCycle.Should().Be(billingCycle);
        result.Amount.Should().Be(amount);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetTrialEndDate_WhenTrialDaysProvided()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = CreateTestPlan(planId);
        const int trialDays = 14;

        _mockPlanService.Setup(p => p.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.CreateAsync(
            tenantId, planId, userId, BillingCycle.Monthly, new Money(29.99m, "USD"),
            trialDays: trialDays
        );

        // Assert
        result.TrialEndDate.Should().NotBeNull();
        result.TrialEndDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(trialDays), TimeSpan.FromSeconds(5));
        result.Status.Should().Be(SubscriptionStatus.Trialing);
    }

    [Fact]
    public async Task CreateAsync_ShouldUseCustomStartDate_WhenProvided()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = CreateTestPlan(planId);
        var startDate = DateTime.UtcNow.AddDays(7);

        _mockPlanService.Setup(p => p.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.CreateAsync(
            tenantId, planId, userId, BillingCycle.Monthly, new Money(29.99m, "USD"),
            startDate: startDate
        );

        // Assert
        result.StartDate.Should().BeCloseTo(startDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenPlanNotFound()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockPlanService.Setup(p => p.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        // Act
        var act = () => _service.CreateAsync(
            tenantId, planId, userId, BillingCycle.Monthly, new Money(29.99m, "USD")
        );

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{planId}*not found*");
    }

    #endregion

    #region ActivateAsync Tests

    [Fact]
    public async Task ActivateAsync_ShouldActivateSubscription_WhenValid()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.ActivateAsync(subscriptionId);

        // Assert
        result.Status.Should().Be(SubscriptionStatus.Active);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_ShouldThrow_WhenSubscriptionNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        var act = () => _service.ActivateAsync(subscriptionId);

        // Assert
        await act.Should().ThrowAsync<SubscriptionNotFoundException>();
    }

    #endregion

    #region StartTrialAsync Tests

    [Fact]
    public async Task StartTrialAsync_ShouldStartTrial_WhenValid()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId);
        const int trialDays = 14;

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.StartTrialAsync(subscriptionId, trialDays);

        // Assert
        result.Status.Should().Be(SubscriptionStatus.Trialing);
        result.TrialEndDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(trialDays), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task StartTrialAsync_ShouldThrow_WhenTrialDaysIsZeroOrNegative()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        // Act
        var act = () => _service.StartTrialAsync(subscriptionId, 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("trialDays");
    }

    [Fact]
    public async Task StartTrialAsync_ShouldThrow_WhenTrialDaysIsNegative()
    {
        // Act
        var act = () => _service.StartTrialAsync(Guid.NewGuid(), -5);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("trialDays");
    }

    #endregion

    #region EndTrialAsync Tests

    [Fact]
    public async Task EndTrialAsync_ShouldConvertToPaid_WhenConvertToPaidIsTrue()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Trialing);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.EndTrialAsync(subscriptionId, convertToPaid: true);

        // Assert
        result.Status.Should().Be(SubscriptionStatus.Active);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EndTrialAsync_ShouldCancel_WhenConvertToPaidIsFalse()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Trialing);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.EndTrialAsync(subscriptionId, convertToPaid: false);

        // Assert - EndTrial(false) goes through Cancel with TrialEnded reason
        result.Status.Should().Be(SubscriptionStatus.Cancelled);
        result.CancellationReason.Should().Be(CancellationReason.TrialEnded);
    }

    #endregion

    #region CancelAsync Tests

    [Fact]
    public async Task CancelAsync_ShouldCancelSubscription_WhenValid()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active);
        var reason = CancellationReason.UserRequested;
        const string note = "Customer requested cancellation";

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.CancelAsync(subscriptionId, reason, note);

        // Assert
        result.Status.Should().Be(SubscriptionStatus.Cancelled);
        result.CancellationReason.Should().Be(reason);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ShouldSetEffectiveDate_WhenProvided()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active);
        var effectiveDate = DateTime.UtcNow.AddDays(30);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.CancelAsync(subscriptionId, CancellationReason.UserRequested, effectiveDate: effectiveDate);

        // Assert
        result.CancelledAt.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ShouldThrow_WhenSubscriptionNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        var act = () => _service.CancelAsync(subscriptionId, CancellationReason.UserRequested);

        // Assert
        await act.Should().ThrowAsync<SubscriptionNotFoundException>();
    }

    #endregion

    #region SuspendAsync Tests

    [Fact]
    public async Task SuspendAsync_ShouldSuspendSubscription_WhenActive()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active);
        const string reason = "Payment failed";

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.SuspendAsync(subscriptionId, reason);

        // Assert
        result.Status.Should().Be(SubscriptionStatus.Suspended);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SuspendAsync_ShouldThrow_WhenSubscriptionNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        var act = () => _service.SuspendAsync(subscriptionId, "reason");

        // Assert
        await act.Should().ThrowAsync<SubscriptionNotFoundException>();
    }

    #endregion

    #region ReactivateAsync Tests

    [Fact]
    public async Task ReactivateAsync_ShouldReactivateSubscription_WhenSuspended()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Suspended);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.ReactivateAsync(subscriptionId);

        // Assert
        result.Status.Should().Be(SubscriptionStatus.Active);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpgradePlanAsync Tests

    [Fact]
    public async Task UpgradePlanAsync_ShouldReturnSuccess_WhenUpgradingToHigherPlan()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var oldPlanId = Guid.NewGuid();
        var newPlanId = Guid.NewGuid();

        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active, oldPlanId);
        var oldPlan = CreateTestPlan(oldPlanId, "Basic", 1999);
        var newPlan = CreateTestPlan(newPlanId, "Premium", 4999);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockPlanService.Setup(p => p.GetByIdAsync(newPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newPlan);
        _mockPlanService.Setup(p => p.GetByIdAsync(oldPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldPlan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.UpgradePlanAsync(subscriptionId, newPlanId);

        // Assert
        result.Success.Should().BeTrue();
        result.UpdatedSubscription.Should().NotBeNull();
        result.UpdatedSubscription!.PlanId.Should().Be(newPlanId);
    }

    [Fact]
    public async Task UpgradePlanAsync_ShouldReturnFailed_WhenDowngradingToLowerPlan()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var oldPlanId = Guid.NewGuid();
        var newPlanId = Guid.NewGuid();

        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active, oldPlanId);
        var oldPlan = CreateTestPlan(oldPlanId, "Premium", 4999);
        var newPlan = CreateTestPlan(newPlanId, "Basic", 1999);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockPlanService.Setup(p => p.GetByIdAsync(newPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newPlan);
        _mockPlanService.Setup(p => p.GetByIdAsync(oldPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldPlan);

        // Act
        var result = await _service.UpgradePlanAsync(subscriptionId, newPlanId);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("not an upgrade");
    }

    [Fact]
    public async Task UpgradePlanAsync_ShouldThrow_WhenSubscriptionNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var newPlanId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        var act = () => _service.UpgradePlanAsync(subscriptionId, newPlanId);

        // Assert
        await act.Should().ThrowAsync<SubscriptionNotFoundException>();
    }

    #endregion

    #region DowngradePlanAsync Tests

    [Fact]
    public async Task DowngradePlanAsync_ShouldReturnSuccess_WhenDowngradingToLowerPlan()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var oldPlanId = Guid.NewGuid();
        var newPlanId = Guid.NewGuid();

        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active, oldPlanId);
        var oldPlan = CreateTestPlan(oldPlanId, "Premium", 4999);
        var newPlan = CreateTestPlan(newPlanId, "Basic", 1999);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockPlanService.Setup(p => p.GetByIdAsync(newPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newPlan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.DowngradePlanAsync(subscriptionId, newPlanId);

        // Assert
        result.Success.Should().BeTrue();
        result.UpdatedSubscription.Should().NotBeNull();
    }

    #endregion

    #region ChangeBillingCycleAsync Tests

    [Fact]
    public async Task ChangeBillingCycleAsync_ShouldUpdateBillingCycle()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active, planId);
        var plan = CreateTestPlan(planId);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockPlanService.Setup(p => p.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.ChangeBillingCycleAsync(subscriptionId, BillingCycle.Annually);

        // Assert
        result.BillingCycle.Should().Be(BillingCycle.Annually);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SetAutoRenewAsync Tests

    [Fact]
    public async Task SetAutoRenewAsync_ShouldSetAutoRenewTrue()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.SetAutoRenewAsync(subscriptionId, true);

        // Assert
        result.AutoRenew.Should().BeTrue();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetAutoRenewAsync_ShouldSetAutoRenewFalse()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.SetAutoRenewAsync(subscriptionId, false);

        // Assert
        result.AutoRenew.Should().BeFalse();
    }

    #endregion

    #region UpdateMetadataAsync Tests

    [Fact]
    public async Task UpdateMetadataAsync_ShouldUpdateMetadata()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active);
        const string metadata = "{\"key\": \"value\"}";

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.UpdateMetadataAsync(subscriptionId, metadata);

        // Assert
        result.Metadata.Should().Be(metadata);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #endregion

    #region ISubscriptionBillingService Tests

    #region ProcessRenewalAsync Tests

    [Fact]
    public async Task ProcessRenewalAsync_ShouldProcessRenewal_WhenValid()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active, planId);
        var plan = CreateTestPlan(planId);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockPlanService.Setup(p => p.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.ProcessRenewalAsync(subscriptionId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessRenewalAsync_ShouldThrow_WhenSubscriptionNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        var act = () => _service.ProcessRenewalAsync(subscriptionId);

        // Assert
        await act.Should().ThrowAsync<SubscriptionNotFoundException>();
    }

    #endregion

    #region RecordPaymentAsync Tests

    [Fact]
    public async Task RecordPaymentAsync_ShouldRecordPayment_WhenValid()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active);
        var paymentDate = DateTime.UtcNow;

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.RecordPaymentAsync(subscriptionId, 29.99m, "USD", paymentDate);

        // Assert
        result.Should().NotBeNull();
        result.LastPaymentAt.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RecordPaymentFailureAsync Tests

    [Fact]
    public async Task RecordPaymentFailureAsync_ShouldRecordFailure()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active);
        var failureDate = DateTime.UtcNow;
        const string reason = "Card declined";

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.RecordPaymentFailureAsync(subscriptionId, reason, failureDate);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ProcessBulkRenewalsAsync Tests

    [Fact]
    public async Task ProcessBulkRenewalsAsync_ShouldProcessAllSubscriptions()
    {
        // Arrange
        var subscriptionIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var planId = Guid.NewGuid();
        var plan = CreateTestPlan(planId);

        foreach (var id in subscriptionIds)
        {
            var subscription = CreateTestSubscription(id, SubscriptionStatus.Active, planId);
            _mockRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);
        }

        _mockPlanService.Setup(p => p.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.ProcessBulkRenewalsAsync(subscriptionIds);

        // Assert
        result.Should().NotBeNull();
        result.TotalProcessed.Should().Be(3);
    }

    [Fact]
    public async Task ProcessBulkRenewalsAsync_ShouldContinueProcessing_WhenSomeSubscriptionsFail()
    {
        // Arrange
        var successId = Guid.NewGuid();
        var failId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var plan = CreateTestPlan(planId);

        var successSubscription = CreateTestSubscription(successId, SubscriptionStatus.Active, planId);
        
        _mockRepository.Setup(r => r.GetByIdAsync(successId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successSubscription);
        _mockRepository.Setup(r => r.GetByIdAsync(failId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _mockPlanService.Setup(p => p.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.ProcessBulkRenewalsAsync(new[] { successId, failId });

        // Assert
        result.TotalProcessed.Should().Be(2);
        result.FailedRenewals.Should().BeGreaterOrEqualTo(1);
    }

    #endregion

    #region SendRenewalRemindersAsync Tests

    [Fact]
    public async Task SendRenewalRemindersAsync_ShouldSendReminders()
    {
        // Arrange
        const int daysBeforeRenewal = 7;
        var subscriptions = new List<Subscription>
        {
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active),
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active)
        };

        _mockRepository.Setup(r => r.GetDueForRenewalAsync(daysBeforeRenewal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);
        _mockNotificationService.Setup(n => n.SendRenewalReminderAsync(It.IsAny<Subscription>(), daysBeforeRenewal, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SendRenewalRemindersAsync(daysBeforeRenewal);

        // Assert
        _mockNotificationService.Verify(
            n => n.SendRenewalReminderAsync(It.IsAny<Subscription>(), daysBeforeRenewal, It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task SendRenewalRemindersAsync_ShouldContinue_WhenNotificationFails()
    {
        // Arrange
        const int daysBeforeRenewal = 7;
        var subscriptions = new List<Subscription>
        {
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active),
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active)
        };

        _mockRepository.Setup(r => r.GetDueForRenewalAsync(daysBeforeRenewal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);
        
        // First call fails, second succeeds
        _mockNotificationService.SetupSequence(n => n.SendRenewalReminderAsync(It.IsAny<Subscription>(), daysBeforeRenewal, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Notification failed"))
            .Returns(Task.CompletedTask);

        // Act - should not throw
        await _service.SendRenewalRemindersAsync(daysBeforeRenewal);

        // Assert
        _mockNotificationService.Verify(
            n => n.SendRenewalReminderAsync(It.IsAny<Subscription>(), daysBeforeRenewal, It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    #endregion

    #region SendTrialExpirationRemindersAsync Tests

    [Fact]
    public async Task SendTrialExpirationRemindersAsync_ShouldSendReminders()
    {
        // Arrange
        const int daysBeforeExpiration = 3;
        var subscriptions = new List<Subscription>
        {
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Trialing),
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Trialing)
        };

        _mockRepository.Setup(r => r.GetTrialsExpiringSoonAsync(daysBeforeExpiration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);
        _mockNotificationService.Setup(n => n.SendTrialExpirationReminderAsync(It.IsAny<Subscription>(), daysBeforeExpiration, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SendTrialExpirationRemindersAsync(daysBeforeExpiration);

        // Assert
        _mockNotificationService.Verify(
            n => n.SendTrialExpirationReminderAsync(It.IsAny<Subscription>(), daysBeforeExpiration, It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task SendTrialExpirationRemindersAsync_ShouldContinue_WhenNotificationFails()
    {
        // Arrange
        const int daysBeforeExpiration = 3;
        var subscriptions = new List<Subscription>
        {
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Trialing),
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Trialing)
        };

        _mockRepository.Setup(r => r.GetTrialsExpiringSoonAsync(daysBeforeExpiration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);
        
        // First call fails, second succeeds
        _mockNotificationService.SetupSequence(n => n.SendTrialExpirationReminderAsync(It.IsAny<Subscription>(), daysBeforeExpiration, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Notification failed"))
            .Returns(Task.CompletedTask);

        // Act - should not throw
        await _service.SendTrialExpirationRemindersAsync(daysBeforeExpiration);

        // Assert
        _mockNotificationService.Verify(
            n => n.SendTrialExpirationReminderAsync(It.IsAny<Subscription>(), daysBeforeExpiration, It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    #endregion

    #endregion

    #region ISubscriptionQueryService Tests

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnSubscription_WhenExists()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var result = await _service.GetByIdAsync(subscriptionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(subscriptionId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await _service.GetByIdAsync(subscriptionId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByExternalIdAsync Tests

    [Fact]
    public async Task GetByExternalIdAsync_ShouldReturnSubscription_WhenExists()
    {
        // Arrange
        const string externalId = "ext_12345";
        var subscription = CreateTestSubscription(Guid.NewGuid());

        _mockRepository.Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var result = await _service.GetByExternalIdAsync(externalId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByExternalIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        const string externalId = "ext_nonexistent";

        _mockRepository.Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await _service.GetByExternalIdAsync(externalId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsSubscriptionActiveAsync Tests

    [Fact]
    public async Task IsSubscriptionActiveAsync_ShouldReturnTrue_WhenActiveSubscriptionExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var subscription = CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active);

        _mockRepository.Setup(r => r.GetActiveTenantSubscriptionAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var result = await _service.IsSubscriptionActiveAsync(tenantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSubscriptionActiveAsync_ShouldReturnFalse_WhenNoActiveSubscription()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetActiveTenantSubscriptionAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await _service.IsSubscriptionActiveAsync(tenantId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSubscriptionActiveAsync_ShouldReturnFalse_WhenSubscriptionNotActive()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var subscription = CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Cancelled);

        _mockRepository.Setup(r => r.GetActiveTenantSubscriptionAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var result = await _service.IsSubscriptionActiveAsync(tenantId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetActiveTenantSubscriptionAsync Tests

    [Fact]
    public async Task GetActiveTenantSubscriptionAsync_ShouldReturnActiveSubscription()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var subscription = CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active);

        _mockRepository.Setup(r => r.GetActiveTenantSubscriptionAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var result = await _service.GetActiveTenantSubscriptionAsync(tenantId);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(SubscriptionStatus.Active);
    }

    #endregion

    #region GetTenantSubscriptionsAsync Tests

    [Fact]
    public async Task GetTenantSubscriptionsAsync_ShouldReturnAllTenantSubscriptions()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var subscriptions = new List<Subscription>
        {
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active),
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Cancelled)
        };

        _mockRepository.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);

        // Act
        var result = await _service.GetTenantSubscriptionsAsync(tenantId);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetTenantSubscriptionHistoryAsync Tests

    [Fact]
    public async Task GetTenantSubscriptionHistoryAsync_ShouldReturnAllSubscriptions()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var subscriptions = new List<Subscription>
        {
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active),
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Cancelled),
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Expired)
        };

        _mockRepository.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);

        // Act
        var result = await _service.GetTenantSubscriptionHistoryAsync(tenantId);

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion

    #region GetExpiringSoonAsync Tests

    [Fact]
    public async Task GetExpiringSoonAsync_ShouldReturnExpiringSoonSubscriptions()
    {
        // Arrange
        const int days = 7;
        var subscriptions = new List<Subscription>
        {
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active)
        };

        _mockRepository.Setup(r => r.GetExpiringSoonAsync(days, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);

        // Act
        var result = await _service.GetExpiringSoonAsync(days);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetDueForRenewalAsync Tests

    [Fact]
    public async Task GetDueForRenewalAsync_ShouldReturnDueForRenewalSubscriptions()
    {
        // Arrange
        const int days = 3;
        var subscriptions = new List<Subscription>
        {
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active)
        };

        _mockRepository.Setup(r => r.GetDueForRenewalAsync(days, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);

        // Act
        var result = await _service.GetDueForRenewalAsync(days);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetTrialsExpiringSoonAsync Tests

    [Fact]
    public async Task GetTrialsExpiringSoonAsync_ShouldReturnExpiringSoonTrials()
    {
        // Arrange
        const int days = 2;
        var subscriptions = new List<Subscription>
        {
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Trialing)
        };

        _mockRepository.Setup(r => r.GetTrialsExpiringSoonAsync(days, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);

        // Act
        var result = await _service.GetTrialsExpiringSoonAsync(days);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region ValidateSubscriptionLimitsAsync Tests

    [Fact]
    public async Task ValidateSubscriptionLimitsAsync_ShouldReturnValid_WhenWithinLimits()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscription = CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active, planId);
        var plan = CreateTestPlan(planId, maxUsers: 100, maxStorageMb: 10000, maxApiCallsPerMonth: 100000);

        _mockRepository.Setup(r => r.GetActiveTenantSubscriptionAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockPlanService.Setup(p => p.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _service.ValidateSubscriptionLimitsAsync(tenantId, userCount: 50, storageMb: 5000, apiCallsPerMonth: 50000);

        // Assert
        result.IsWithinLimits.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSubscriptionLimitsAsync_ShouldReturnInvalid_WhenNoSubscription()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetActiveTenantSubscriptionAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await _service.ValidateSubscriptionLimitsAsync(tenantId, userCount: 10, storageMb: 100, apiCallsPerMonth: 1000);

        // Assert
        result.IsWithinLimits.Should().BeFalse();
        result.RecommendedAction.Should().Contain("subscribe");
    }

    [Fact]
    public async Task ValidateSubscriptionLimitsAsync_ShouldReturnInvalid_WhenUserLimitExceeded()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscription = CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active, planId);
        var plan = CreateTestPlan(planId, maxUsers: 10, maxStorageMb: 10000, maxApiCallsPerMonth: 100000);

        _mockRepository.Setup(r => r.GetActiveTenantSubscriptionAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockPlanService.Setup(p => p.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _service.ValidateSubscriptionLimitsAsync(tenantId, userCount: 50, storageMb: 5000, apiCallsPerMonth: 50000);

        // Assert
        result.IsWithinLimits.Should().BeFalse();
        result.LimitChecks.Should().Contain(c => c.LimitName == "Users");
    }

    [Fact]
    public async Task ValidateSubscriptionLimitsAsync_ShouldReturnInvalid_WhenStorageLimitExceeded()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscription = CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active, planId);
        var plan = CreateTestPlan(planId, maxUsers: 100, maxStorageMb: 1000, maxApiCallsPerMonth: 100000);

        _mockRepository.Setup(r => r.GetActiveTenantSubscriptionAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockPlanService.Setup(p => p.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _service.ValidateSubscriptionLimitsAsync(tenantId, userCount: 50, storageMb: 5000, apiCallsPerMonth: 50000);

        // Assert
        result.IsWithinLimits.Should().BeFalse();
        result.LimitChecks.Should().Contain(c => c.LimitName == "Storage");
    }

    [Fact]
    public async Task ValidateSubscriptionLimitsAsync_ShouldReturnInvalid_WhenApiCallsLimitExceeded()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscription = CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active, planId);
        var plan = CreateTestPlan(planId, maxUsers: 100, maxStorageMb: 10000, maxApiCallsPerMonth: 10000);

        _mockRepository.Setup(r => r.GetActiveTenantSubscriptionAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockPlanService.Setup(p => p.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _service.ValidateSubscriptionLimitsAsync(tenantId, userCount: 50, storageMb: 5000, apiCallsPerMonth: 50000);

        // Assert
        result.IsWithinLimits.Should().BeFalse();
        result.LimitChecks.Should().Contain(c => c.LimitName == "API Calls");
    }

    #endregion

    #region GetUsageStatisticsAsync Tests

    [Fact]
    public async Task GetUsageStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active, planId);
        var plan = CreateTestPlan(planId, maxUsers: 100, maxStorageMb: 10000, maxApiCallsPerMonth: 100000);

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockPlanService.Setup(p => p.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _service.GetUsageStatisticsAsync(subscriptionId);

        // Assert
        result.Should().NotBeNull();
        result.SubscriptionId.Should().Be(subscriptionId);
        result.PlanLimits.MaxUsers.Should().Be(100);
    }

    [Fact]
    public async Task GetUsageStatisticsAsync_ShouldThrow_WhenSubscriptionNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        var act = () => _service.GetUsageStatisticsAsync(subscriptionId);

        // Assert
        await act.Should().ThrowAsync<SubscriptionNotFoundException>();
    }

    #endregion

    #region GetRevenueAnalyticsAsync Tests

    [Fact]
    public async Task GetRevenueAnalyticsAsync_ShouldReturnAnalytics()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var subscriptions = new List<Subscription>
        {
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active),
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active)
        };

        _mockRepository.Setup(r => r.GetByDateRangeAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);

        // Act
        var result = await _service.GetRevenueAnalyticsAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.PeriodStart.Should().Be(startDate);
        result.PeriodEnd.Should().Be(endDate);
    }

    #endregion

    #region GetSubscriptionAnalyticsAsync Tests

    [Fact]
    public async Task GetSubscriptionAnalyticsAsync_ShouldReturnAnalytics()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var statusCounts = new Dictionary<SubscriptionStatus, int>
        {
            { SubscriptionStatus.Active, 100 },
            { SubscriptionStatus.Trialing, 20 },
            { SubscriptionStatus.Cancelled, 10 },
            { SubscriptionStatus.Suspended, 5 }
        };
        var subscriptions = new List<Subscription>
        {
            CreateTestSubscription(Guid.NewGuid(), SubscriptionStatus.Active)
        };

        _mockRepository.Setup(r => r.GetCountByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(statusCounts);
        _mockRepository.Setup(r => r.GetByDateRangeAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);

        // Act
        var result = await _service.GetSubscriptionAnalyticsAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.TotalSubscriptions.Should().Be(135);
        result.ActiveSubscriptions.Should().Be(100);
        result.TrialingSubscriptions.Should().Be(20);
        result.CancelledSubscriptions.Should().Be(10);
        result.SuspendedSubscriptions.Should().Be(5);
    }

    #endregion

    #endregion

    #region ISubscriptionExternalIdService Tests

    #region SetExternalIdsAsync Tests

    [Fact]
    public async Task SetExternalIdsAsync_ShouldSetExternalIds()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateTestSubscription(subscriptionId, SubscriptionStatus.Active);
        const string externalSubscriptionId = "sub_12345";
        const string externalCustomerId = "cus_67890";

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _service.SetExternalIdsAsync(subscriptionId, externalSubscriptionId, externalCustomerId);

        // Assert
        result.ExternalId.Should().Be(externalSubscriptionId);
        result.ExternalCustomerId.Should().Be(externalCustomerId);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetExternalIdsAsync_ShouldThrow_WhenSubscriptionNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        var act = () => _service.SetExternalIdsAsync(subscriptionId, "sub_12345", "cus_67890");

        // Assert
        await act.Should().ThrowAsync<SubscriptionNotFoundException>();
    }

    #endregion

    #region GetByExternalIdAsync (explicit interface) Tests

    [Fact]
    public async Task ISubscriptionExternalIdService_GetByExternalIdAsync_ShouldReturnSubscription()
    {
        // Arrange
        const string externalId = "ext_12345";
        var subscription = CreateTestSubscription(Guid.NewGuid());

        _mockRepository.Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var service = (ISubscriptionExternalIdService)_service;
        var result = await service.GetByExternalIdAsync(externalId);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #endregion

    #region Helper Methods

    private static Subscription CreateTestSubscription(
        Guid subscriptionId,
        SubscriptionStatus status = SubscriptionStatus.PendingActivation,
        Guid? planId = null)
    {
        // Don't set trialEndDate in constructor for Trialing status - the constructor already sets status to Trialing
        // We'll let the natural state be determined by the constructor
        var needsTrialInConstructor = status == SubscriptionStatus.Trialing || status == SubscriptionStatus.Expired;
        
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: planId ?? Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(29.99m, "USD"),
            startDate: DateTime.UtcNow,
            trialEndDate: needsTrialInConstructor ? DateTime.UtcNow.AddDays(14) : null
        );

        // Use reflection to set the ID for testing
        typeof(EntityBase).GetProperty("Id")!.SetValue(subscription, subscriptionId);
        
        // Activate the subscription to required status through valid state transitions
        if (status == SubscriptionStatus.Active)
        {
            subscription.Activate();
        }
        // Trialing is already set by constructor when trialEndDate is passed, so no action needed
        else if (status == SubscriptionStatus.Suspended)
        {
            subscription.Activate();
            subscription.Suspend("Test suspension");
        }
        else if (status == SubscriptionStatus.Cancelled)
        {
            subscription.Cancel(CancellationReason.UserRequested);
        }
        else if (status == SubscriptionStatus.Expired)
        {
            // Already in Trialing from constructor, now end the trial without converting to paid
            subscription.EndTrial(convertToPaid: false);
        }

        return subscription;
    }

    private static SubscriptionPlan CreateTestPlan(
        Guid planId,
        string name = "Test Plan",
        long priceInCents = 2999,
        int? maxUsers = null,
        long? maxStorageMb = null,
        long? maxApiCallsPerMonth = null)
    {
        var plan = new SubscriptionPlan(name, $"{name.ToLower().Replace(" ", "-")}", priceInCents)
        {
            AnnualPriceInCents = priceInCents * 10 // 2 months free annually
        };

        // Use reflection to set the ID for testing
        typeof(EntityBase).GetProperty("Id")!.SetValue(plan, planId);

        // Set limits using UpdateLimits method
        if (maxUsers.HasValue || maxStorageMb.HasValue || maxApiCallsPerMonth.HasValue)
        {
            plan.UpdateLimits(maxUsers, maxStorageMb, maxApiCallsPerMonth);
        }

        return plan;
    }

    #endregion
}
