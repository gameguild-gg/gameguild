using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Services;

public class SubscriptionPlanServiceTests
{
    private readonly Mock<ISubscriptionPlanRepository> _mockRepository;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly SubscriptionPlanService _service;

    public SubscriptionPlanServiceTests()
    {
        _mockRepository = new Mock<ISubscriptionPlanRepository>();
        _mockCache = new Mock<IMemoryCache>();

        // Setup cache to return false for TryGetValue (cache miss)
        object? cacheEntry = null;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheEntry)).Returns(false);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        _service = new SubscriptionPlanService(_mockRepository.Object, _mockCache.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ShouldCreatePlan_WhenNameAndSlugAreUnique()
    {
        // Arrange
        const string name = "Premium Plan";
        const string slug = "premium-plan";
        const long priceInCents = 2999;

        _mockRepository.Setup(r => r.IsNameUniqueAsync(name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.IsSlugUniqueAsync(slug, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan p, CancellationToken _) => p);

        // Act
        var result = await _service.CreateAsync(name, slug, priceInCents);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Slug.Should().Be(slug);
        result.MonthlyPriceInCents.Should().Be(priceInCents);
        result.Currency.Should().Be("USD");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenNameIsNotUnique()
    {
        // Arrange
        const string name = "Existing Plan";
        const string slug = "new-slug";

        _mockRepository.Setup(r => r.IsNameUniqueAsync(name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.CreateAsync(name, slug, 2999);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*name '{name}'*already exists*");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenSlugIsNotUnique()
    {
        // Arrange
        const string name = "New Plan";
        const string slug = "existing-slug";

        _mockRepository.Setup(r => r.IsNameUniqueAsync(name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.IsSlugUniqueAsync(slug, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.CreateAsync(name, slug, 2999);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*slug '{slug}'*already exists*");
    }

    [Fact]
    public async Task CreateAsync_ShouldSetCurrencyAndDescription_WhenProvided()
    {
        // Arrange
        const string name = "Euro Plan";
        const string slug = "euro-plan";
        const string currency = "EUR";
        const string description = "Plan for European customers";

        _mockRepository.Setup(r => r.IsNameUniqueAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.IsSlugUniqueAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan p, CancellationToken _) => p);

        // Act
        var result = await _service.CreateAsync(name, slug, 2999, currency, description);

        // Assert
        result.Currency.Should().Be(currency);
        result.Description.Should().Be(description);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ShouldUpdatePlan_WhenPlanExists()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var existingPlan = new SubscriptionPlan("Old Name", "old-slug", 1999);
        const string newName = "New Name";

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);
        _mockRepository.Setup(r => r.IsNameUniqueAsync(newName, planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan p, CancellationToken _) => p);

        // Act
        var result = await _service.UpdateAsync(planId, newName, "New description", 5);

        // Assert
        result.Name.Should().Be(newName);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenPlanNotFound()
    {
        // Arrange
        var planId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        // Act
        var act = () => _service.UpdateAsync(planId, "New Name");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*'{planId}'*not found*");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenNewNameIsNotUnique()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var existingPlan = new SubscriptionPlan("Old Name", "old-slug", 1999);
        const string newName = "Taken Name";

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);
        _mockRepository.Setup(r => r.IsNameUniqueAsync(newName, planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.UpdateAsync(planId, newName);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*name '{newName}'*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_ShouldSkipUniquenessCheck_WhenNameUnchanged()
    {
        // Arrange
        var planId = Guid.NewGuid();
        const string sameName = "Same Name";
        var existingPlan = new SubscriptionPlan(sameName, "same-slug", 1999);

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan p, CancellationToken _) => p);

        // Act
        await _service.UpdateAsync(planId, sameName, "Updated description");

        // Assert - Should not check uniqueness since name hasn't changed
        _mockRepository.Verify(r => r.IsNameUniqueAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UpdatePricingAsync Tests

    [Fact]
    public async Task UpdatePricingAsync_ShouldUpdatePrices_WhenPlanExists()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var existingPlan = new SubscriptionPlan("Plan", "plan", 1999);
        const long newMonthlyPrice = 2999;
        const long newAnnualPrice = 29990;

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan p, CancellationToken _) => p);

        // Act
        var result = await _service.UpdatePricingAsync(planId, newMonthlyPrice, newAnnualPrice);

        // Assert
        result.MonthlyPriceInCents.Should().Be(newMonthlyPrice);
        result.AnnualPriceInCents.Should().Be(newAnnualPrice);
    }

    #endregion

    #region UpdateLimitsAsync Tests

    [Fact]
    public async Task UpdateLimitsAsync_ShouldUpdateLimits_WhenPlanExists()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var existingPlan = new SubscriptionPlan("Plan", "plan", 1999);
        const int maxUsers = 100;
        const long maxStorage = 10240;
        const long maxApiCalls = 1000000;

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan p, CancellationToken _) => p);

        // Act
        var result = await _service.UpdateLimitsAsync(planId, maxUsers, maxStorage, maxApiCalls);

        // Assert
        result.MaxUsers.Should().Be(maxUsers);
        result.MaxStorageMb.Should().Be(maxStorage);
        result.MaxApiCallsPerMonth.Should().Be(maxApiCalls);
    }

    #endregion

    #region ActivateAsync / DeactivateAsync Tests

    [Fact]
    public async Task ActivateAsync_ShouldActivatePlan_WhenPlanExists()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var existingPlan = new SubscriptionPlan("Plan", "plan", 1999);
        existingPlan.Deactivate();

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan p, CancellationToken _) => p);

        // Act
        var result = await _service.ActivateAsync(planId);

        // Assert
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_ShouldDeactivatePlan_WhenPlanExists()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var existingPlan = new SubscriptionPlan("Plan", "plan", 1999);

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan p, CancellationToken _) => p);

        // Act
        var result = await _service.DeactivateAsync(planId);

        // Assert
        result.IsActive.Should().BeFalse();
    }

    #endregion

    #region GetByIdAsync / GetBySlugAsync Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPlan_WhenExists()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var expectedPlan = new SubscriptionPlan("Test Plan", "test-plan", 999);

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPlan);

        // Act
        var result = await _service.GetByIdAsync(planId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Plan");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var planId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        // Act
        var result = await _service.GetByIdAsync(planId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySlugAsync_ShouldReturnPlan_WhenExists()
    {
        // Arrange
        const string slug = "premium-plan";
        var expectedPlan = new SubscriptionPlan("Premium Plan", slug, 2999);

        _mockRepository.Setup(r => r.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPlan);

        // Act
        var result = await _service.GetBySlugAsync(slug);

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be(slug);
    }

    #endregion

    #region GetActiveAsync / GetFeaturedAsync Tests

    [Fact]
    public async Task GetActiveAsync_ShouldReturnActivePlans()
    {
        // Arrange
        var plans = new List<SubscriptionPlan>
        {
            new("Basic", "basic", 999),
            new("Pro", "pro", 1999)
        };

        _mockRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        // Act
        var result = await _service.GetActiveAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFeaturedAsync_ShouldReturnFeaturedPlans()
    {
        // Arrange
        var featuredPlan = new SubscriptionPlan("Featured", "featured", 1999);
        featuredPlan.SetFeatured(true);

        _mockRepository.Setup(r => r.GetFeaturedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { featuredPlan });

        // Act
        var result = await _service.GetFeaturedAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().IsFeatured.Should().BeTrue();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ShouldDeletePlan_WhenNoActiveSubscriptions()
    {
        // Arrange
        var planId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetActiveSubscriptionCountAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _service.DeleteAsync(planId);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(planId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenPlanHasActiveSubscriptions()
    {
        // Arrange
        var planId = Guid.NewGuid();
        const int activeSubscriptions = 5;

        _mockRepository.Setup(r => r.GetActiveSubscriptionCountAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeSubscriptions);

        // Act
        var act = () => _service.DeleteAsync(planId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{activeSubscriptions} active subscriptions*");
    }

    #endregion

    #region ValidatePlanLimitsAsync Tests

    [Fact]
    public async Task ValidatePlanLimitsAsync_ShouldReturnSuccess_WhenWithinLimits()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Pro Plan", "pro-plan", 2999);
        plan.UpdateLimits(maxUsers: 100, maxStorageMb: 10240, maxApiCallsPerMonth: 1000000);

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _service.ValidatePlanLimitsAsync(planId, userCount: 50, storageMb: 5000, apiCallsPerMonth: 500000);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidatePlanLimitsAsync_ShouldReturnFailure_WhenExceedsUserLimit()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Basic Plan", "basic-plan", 999);
        plan.UpdateLimits(maxUsers: 10, maxStorageMb: 1024, maxApiCallsPerMonth: 10000);

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SubscriptionPlan>());

        // Act
        var result = await _service.ValidatePlanLimitsAsync(planId, userCount: 50, storageMb: 500, apiCallsPerMonth: 5000);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("max 10 users"));
    }

    [Fact]
    public async Task ValidatePlanLimitsAsync_ShouldSuggestUpgrades_WhenExceedsLimits()
    {
        // Arrange
        var basicPlanId = Guid.NewGuid();
        var basicPlan = new SubscriptionPlan("Basic Plan", "basic-plan", 999);
        basicPlan.UpdateLimits(maxUsers: 10, maxStorageMb: 1024, maxApiCallsPerMonth: 10000);

        var proPlan = new SubscriptionPlan("Pro Plan", "pro-plan", 2999);
        proPlan.UpdateLimits(maxUsers: 100, maxStorageMb: 10240, maxApiCallsPerMonth: 100000);

        _mockRepository.Setup(r => r.GetByIdAsync(basicPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basicPlan);
        _mockRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { basicPlan, proPlan });

        // Act
        var result = await _service.ValidatePlanLimitsAsync(basicPlanId, userCount: 50, storageMb: 5000, apiCallsPerMonth: 50000);

        // Assert
        result.IsValid.Should().BeFalse();
        result.SuggestedUpgrades.Should().Contain(proPlan.Id);
    }

    #endregion

    #region SuggestUpgradesAsync Tests

    [Fact]
    public async Task SuggestUpgradesAsync_ShouldReturnHigherPricedPlans_ThatMeetRequirements()
    {
        // Arrange
        var currentPlanId = Guid.NewGuid();
        var currentPlan = new SubscriptionPlan("Basic", "basic", 999);
        currentPlan.UpdateLimits(maxUsers: 10, maxStorageMb: 1024, maxApiCallsPerMonth: 10000);

        var proPlan = new SubscriptionPlan("Pro", "pro", 2999);
        proPlan.UpdateLimits(maxUsers: 50, maxStorageMb: 5120, maxApiCallsPerMonth: 100000);

        var enterprisePlan = new SubscriptionPlan("Enterprise", "enterprise", 9999);
        enterprisePlan.UpdateLimits(maxUsers: null, maxStorageMb: null, maxApiCallsPerMonth: null); // Unlimited

        _mockRepository.Setup(r => r.GetByIdAsync(currentPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPlan);
        _mockRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { currentPlan, proPlan, enterprisePlan });

        // Act
        var result = await _service.SuggestUpgradesAsync(currentPlanId, currentUserCount: 25, currentStorageMb: 2048, currentApiCallsPerMonth: 50000);

        // Assert
        var upgrades = result.ToList();
        upgrades.Should().NotContain(p => p.Id == currentPlanId); // Should not suggest current plan
        upgrades.Should().Contain(p => p.Name == "Pro");
        upgrades.Should().Contain(p => p.Name == "Enterprise");
        upgrades.First().MonthlyPriceInCents.Should().BeLessThan(upgrades.Last().MonthlyPriceInCents); // Ordered by price
    }

    #endregion

    #region SetFeaturedAsync / SetExternalIdAsync Tests

    [Fact]
    public async Task SetFeaturedAsync_ShouldSetFeaturedFlag()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Plan", "plan", 1999);

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan p, CancellationToken _) => p);

        // Act
        var result = await _service.SetFeaturedAsync(planId, true);

        // Assert
        result.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public async Task SetExternalIdAsync_ShouldSetExternalId()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Plan", "plan", 1999);
        const string externalId = "price_stripe_123";

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan p, CancellationToken _) => p);

        // Act
        var result = await _service.SetExternalIdAsync(planId, externalId);

        // Assert
        result.ExternalId.Should().Be(externalId);
    }

    #endregion

    #region UpdateFeaturesAsync Tests

    [Fact]
    public async Task UpdateFeaturesAsync_ShouldUpdateFeatureFlags()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Plan", "plan", 1999);

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan p, CancellationToken _) => p);

        // Act
        var result = await _service.UpdateFeaturesAsync(
            planId,
            hasPrioritySupport: true,
            hasAdvancedAnalytics: true,
            hasCustomBranding: true,
            features: "[\"feature1\",\"feature2\"]"
        );

        // Assert
        result.HasPrioritySupport.Should().BeTrue();
        result.HasAdvancedAnalytics.Should().BeTrue();
        result.HasCustomBranding.Should().BeTrue();
        result.Features.Should().Be("[\"feature1\",\"feature2\"]");
    }

    #endregion

    #region SearchAsync / GetByPriceRangeAsync Tests

    [Fact]
    public async Task SearchAsync_ShouldDelegateToRepository()
    {
        // Arrange
        const string searchTerm = "premium";
        var plans = new[] { new SubscriptionPlan("Premium Plan", "premium-plan", 2999) };

        _mockRepository.Setup(r => r.SearchByNameAsync(searchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        // Act
        var result = await _service.SearchAsync(searchTerm);

        // Assert
        result.Should().HaveCount(1);
        _mockRepository.Verify(r => r.SearchByNameAsync(searchTerm, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByPriceRangeAsync_ShouldDelegateToRepository()
    {
        // Arrange
        const long minPrice = 1000;
        const long maxPrice = 5000;
        var plans = new[]
        {
            new SubscriptionPlan("Basic", "basic", 1999),
            new SubscriptionPlan("Pro", "pro", 3999)
        };

        _mockRepository.Setup(r => r.GetByPriceRangeAsync(minPrice, maxPrice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        // Act
        var result = await _service.GetByPriceRangeAsync(minPrice, maxPrice);

        // Assert
        result.Should().HaveCount(2);
        _mockRepository.Verify(r => r.GetByPriceRangeAsync(minPrice, maxPrice, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetUsageStatisticsAsync Tests

    [Fact]
    public async Task GetUsageStatisticsAsync_ShouldReturnStatistics_WhenPlanExists()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Pro Plan", "pro-plan", 2999);
        const int activeSubscriptionCount = 42;

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.GetActiveSubscriptionCountAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeSubscriptionCount);

        // Act
        var result = await _service.GetUsageStatisticsAsync(planId);

        // Assert
        result.Should().NotBeNull();
        result.PlanId.Should().Be(planId);
        result.ActiveSubscriptions.Should().Be(activeSubscriptionCount);
        result.AverageMonthlyRevenue.Should().Be(activeSubscriptionCount * 29.99m);
    }

    [Fact]
    public async Task GetUsageStatisticsAsync_ShouldThrow_WhenPlanNotFound()
    {
        // Arrange
        var planId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        // Act
        var act = () => _service.GetUsageStatisticsAsync(planId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*'{planId}'*not found*");
    }

    #endregion

    #region Caching Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFromCache_WhenCacheHit()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var cachedPlan = new SubscriptionPlan("Cached Plan", "cached-plan", 999);
        object cachedValue = cachedPlan;

        var mockCache = new Mock<IMemoryCache>();
        mockCache.Setup(c => c.TryGetValue($"SubscriptionPlans:ById:{planId}", out cachedValue!)).Returns(true);

        var service = new SubscriptionPlanService(_mockRepository.Object, mockCache.Object);

        // Act
        var result = await service.GetByIdAsync(planId);

        // Assert
        result.Should().Be(cachedPlan);
        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetActiveAsync_ShouldReturnFromCache_WhenCacheHit()
    {
        // Arrange
        var cachedPlans = new List<SubscriptionPlan>
        {
            new("Basic", "basic", 999),
            new("Pro", "pro", 1999)
        };
        object cachedValue = cachedPlans.AsEnumerable();

        var mockCache = new Mock<IMemoryCache>();
        mockCache.Setup(c => c.TryGetValue("SubscriptionPlans:Active", out cachedValue!)).Returns(true);

        var service = new SubscriptionPlanService(_mockRepository.Object, mockCache.Object);

        // Act
        var result = await service.GetActiveAsync();

        // Assert
        result.Should().HaveCount(2);
        _mockRepository.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldPopulateCache_OnCacheMiss()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Test Plan", "test-plan", 999);
        
        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        await _service.GetByIdAsync(planId);

        // Assert - Should call repository and set cache
        _mockRepository.Verify(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.CreateEntry(It.Is<object>(k => k.ToString()!.Contains(planId.ToString()))), Times.Once);
    }

    [Fact]
    public async Task GetActiveAsync_ShouldPopulateCache_OnCacheMiss()
    {
        // Arrange
        var plans = new List<SubscriptionPlan>
        {
            new("Basic", "basic", 999)
        };
        
        _mockRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        // Act
        await _service.GetActiveAsync();

        // Assert - Should call repository and set cache
        _mockRepository.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.CreateEntry("SubscriptionPlans:Active"), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldInvalidateActiveCache()
    {
        // Arrange
        _mockRepository.Setup(r => r.IsNameUniqueAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.IsSlugUniqueAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan p, CancellationToken _) => p);

        // Act
        await _service.CreateAsync("New Plan", "new-plan", 1999);

        // Assert - Should remove active plans from cache
        _mockCache.Verify(c => c.Remove("SubscriptionPlans:Active"), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldInvalidatePlanAndActiveCache()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Old Name", "old-slug", 1999);
        
        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.IsNameUniqueAsync(It.IsAny<string>(), planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan p, CancellationToken _) => p);

        // Act
        await _service.UpdateAsync(planId, "New Name");

        // Assert - Should invalidate both caches
        _mockCache.Verify(c => c.Remove($"SubscriptionPlans:ById:{planId}"), Times.Once);
        _mockCache.Verify(c => c.Remove("SubscriptionPlans:Active"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldInvalidatePlanAndActiveCache()
    {
        // Arrange
        var planId = Guid.NewGuid();
        
        _mockRepository.Setup(r => r.GetActiveSubscriptionCountAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _service.DeleteAsync(planId);

        // Assert - Should invalidate both caches
        _mockCache.Verify(c => c.Remove($"SubscriptionPlans:ById:{planId}"), Times.Once);
        _mockCache.Verify(c => c.Remove("SubscriptionPlans:Active"), Times.Once);
    }

    #endregion

    #region ValidatePlanLimitsAsync Additional Tests

    [Fact]
    public async Task ValidatePlanLimitsAsync_ShouldReturnFailure_WhenExceedsStorageLimit()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Basic Plan", "basic-plan", 999);
        plan.UpdateLimits(maxUsers: 100, maxStorageMb: 1024, maxApiCallsPerMonth: 100000);

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SubscriptionPlan>());

        // Act
        var result = await _service.ValidatePlanLimitsAsync(planId, userCount: 5, storageMb: 5000, apiCallsPerMonth: 1000);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("max 1024MB storage"));
    }

    [Fact]
    public async Task ValidatePlanLimitsAsync_ShouldReturnFailure_WhenExceedsApiCallsLimit()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Basic Plan", "basic-plan", 999);
        plan.UpdateLimits(maxUsers: 100, maxStorageMb: 10240, maxApiCallsPerMonth: 10000);

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SubscriptionPlan>());

        // Act
        var result = await _service.ValidatePlanLimitsAsync(planId, userCount: 5, storageMb: 1000, apiCallsPerMonth: 50000);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("max 10000 API calls"));
    }

    [Fact]
    public async Task ValidatePlanLimitsAsync_ShouldReturnMultipleErrors_WhenMultipleLimitsExceeded()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Basic Plan", "basic-plan", 999);
        plan.UpdateLimits(maxUsers: 5, maxStorageMb: 512, maxApiCallsPerMonth: 1000);

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SubscriptionPlan>());

        // Act
        var result = await _service.ValidatePlanLimitsAsync(planId, userCount: 50, storageMb: 5000, apiCallsPerMonth: 50000);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(e => e.Contains("max 5 users"));
        result.Errors.Should().Contain(e => e.Contains("max 512MB storage"));
        result.Errors.Should().Contain(e => e.Contains("max 1000 API calls"));
    }

    [Fact]
    public async Task ValidatePlanLimitsAsync_ShouldAllowUnlimited_WhenLimitIsNull()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Enterprise Plan", "enterprise-plan", 9999);
        // No limits set (all null = unlimited)

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _service.ValidatePlanLimitsAsync(planId, userCount: 10000, storageMb: 1000000, apiCallsPerMonth: 100000000);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region GetBySlugAsync Tests

    [Fact]
    public async Task GetBySlugAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        const string slug = "nonexistent-plan";

        _mockRepository.Setup(r => r.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        // Act
        var result = await _service.GetBySlugAsync(slug);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ActivateAsync / DeactivateAsync Additional Tests

    [Fact]
    public async Task ActivateAsync_ShouldThrow_WhenPlanNotFound()
    {
        // Arrange
        var planId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        // Act
        var act = () => _service.ActivateAsync(planId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*'{planId}'*not found*");
    }

    [Fact]
    public async Task DeactivateAsync_ShouldThrow_WhenPlanNotFound()
    {
        // Arrange
        var planId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        // Act
        var act = () => _service.DeactivateAsync(planId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*'{planId}'*not found*");
    }

    #endregion
}
