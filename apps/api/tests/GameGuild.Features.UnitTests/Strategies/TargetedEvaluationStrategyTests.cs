using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Features.UnitTests.Strategies;

/// <summary>
/// Tests for TargetedEvaluationStrategy verifying targeting chain priority order.
/// </summary>
public class TargetedEvaluationStrategyTests
{
    [Fact]
    public void FeatureType_ReturnsUserSegment()
    {
        // Arrange
        var handlers = new List<ITargetingRuleHandler>();
        var strategy = new TargetedEvaluationStrategy(handlers);

        // Assert
        strategy.FeatureType.Should().Be(FeatureFlagType.UserSegment);
    }

    [Fact]
    public async Task TargetingChain_PriorityOrderRespected_HandlersCalledInOrder()
    {
        // Arrange
        var callOrder = new List<int>();
        
        var handler1 = CreateMockHandler(priority: 1, returnValue: null, callOrder);
        var handler2 = CreateMockHandler(priority: 2, returnValue: null, callOrder);
        var handler3 = CreateMockHandler(priority: 3, returnValue: null, callOrder);
        var handler4 = CreateMockHandler(priority: 4, returnValue: null, callOrder);
        var handler5 = CreateMockHandler(priority: 5, returnValue: null, callOrder);

        // Add handlers out of order to verify sorting
        var handlers = new List<ITargetingRuleHandler>
        {
            handler3.Object,
            handler1.Object,
            handler5.Object,
            handler2.Object,
            handler4.Object
        };

        var strategy = new TargetedEvaluationStrategy(handlers);
        var featureFlag = CreateFeatureFlagWithTargets("test-feature");
        var context = new FeatureContext { TenantId = Guid.NewGuid() };

        // Act
        await strategy.EvaluateAsync(featureFlag, context);

        // Assert - Handlers should be called in priority order (1, 2, 3, 4, 5)
        callOrder.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 }, 
            options => options.WithStrictOrdering(),
            "Handlers should be called in priority order");
    }

    [Fact]
    public async Task TargetingChain_StopsOnFirstMatch()
    {
        // Arrange
        var callOrder = new List<int>();
        
        var handler1 = CreateMockHandler(priority: 1, returnValue: null, callOrder);
        var handler2 = CreateMockHandler(priority: 2, returnValue: new FeatureEvaluationResult { IsEnabled = true, Reason = "Handler 2 matched" }, callOrder);
        var handler3 = CreateMockHandler(priority: 3, returnValue: null, callOrder);

        var handlers = new List<ITargetingRuleHandler>
        {
            handler1.Object,
            handler2.Object,
            handler3.Object
        };

        var strategy = new TargetedEvaluationStrategy(handlers);
        var featureFlag = CreateFeatureFlagWithTargets("test-feature");
        var context = new FeatureContext { TenantId = Guid.NewGuid() };

        // Act
        var result = await strategy.EvaluateAsync(featureFlag, context);

        // Assert
        callOrder.Should().BeEquivalentTo(new[] { 1, 2 }, 
            "Chain should stop after handler 2 returns a result");
        result.IsEnabled.Should().BeTrue();
        result.Reason.Should().Contain("Handler 2");
    }

    [Fact]
    public void TargetingChain_TenantHandlerHasHighestPriority()
    {
        // Arrange - Create real handler instances to verify their priorities
        var tenantHandler = new TenantTargetingHandler(Mock.Of<Microsoft.Extensions.Logging.ILogger<TenantTargetingHandler>>());
        var userHandler = new UserTargetingHandler();
        var planHandler = new PlanTargetingHandler();
        var countryHandler = new CountryTargetingHandler();
        var customHandler = new CustomTargetingHandler();

        // Act - Get priorities
        var handlers = new ITargetingRuleHandler[]
        {
            tenantHandler,
            userHandler,
            planHandler,
            countryHandler,
            customHandler
        };

        var sortedHandlers = handlers.OrderBy(h => h.Priority).ToList();

        // Assert - Tenant should be first (priority 1)
        sortedHandlers[0].Should().BeOfType<TenantTargetingHandler>("Tenant handler should have highest priority");
        sortedHandlers[0].Priority.Should().Be(1);
        
        // User should be second (priority 2)
        sortedHandlers[1].Should().BeOfType<UserTargetingHandler>("User handler should be second");
        sortedHandlers[1].Priority.Should().Be(2);
        
        // Plan should be third (priority 3)
        sortedHandlers[2].Should().BeOfType<PlanTargetingHandler>("Plan handler should be third");
        sortedHandlers[2].Priority.Should().Be(3);
        
        // Country should be fourth (priority 4)
        sortedHandlers[3].Should().BeOfType<CountryTargetingHandler>("Country handler should be fourth");
        sortedHandlers[3].Priority.Should().Be(4);
        
        // Custom should be last (priority 5)
        sortedHandlers[4].Should().BeOfType<CustomTargetingHandler>("Custom handler should have lowest priority");
        sortedHandlers[4].Priority.Should().Be(5);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsDisabled_WhenNoHandlersMatch()
    {
        // Arrange
        var handler1 = CreateMockHandler(priority: 1, returnValue: null, new List<int>());
        var handler2 = CreateMockHandler(priority: 2, returnValue: null, new List<int>());

        var handlers = new List<ITargetingRuleHandler>
        {
            handler1.Object,
            handler2.Object
        };

        var strategy = new TargetedEvaluationStrategy(handlers);
        var featureFlag = CreateFeatureFlagWithTargets("test-feature");
        var context = new FeatureContext { TenantId = Guid.NewGuid() };

        // Act
        var result = await strategy.EvaluateAsync(featureFlag, context);

        // Assert
        result.IsEnabled.Should().BeFalse("When no handlers match, feature should be disabled");
        result.Reason.Should().Contain("No targeting rules matched");
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsEnabled_WhenNoTargetingRules()
    {
        // Arrange
        var handlers = new List<ITargetingRuleHandler>();
        var strategy = new TargetedEvaluationStrategy(handlers);
        var featureFlag = CreateFeatureFlag("no-targets"); // No targets
        var context = new FeatureContext { TenantId = Guid.NewGuid() };

        // Act
        var result = await strategy.EvaluateAsync(featureFlag, context);

        // Assert
        result.IsEnabled.Should().BeTrue("Feature with no targeting rules should be enabled for all");
        result.Reason.Should().Contain("No targeting rules defined");
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsDisabled_WhenFeatureIsDisabled()
    {
        // Arrange
        var handler = CreateMockHandler(priority: 1, 
            returnValue: new FeatureEvaluationResult { IsEnabled = true }, 
            new List<int>());

        var handlers = new List<ITargetingRuleHandler> { handler.Object };
        var strategy = new TargetedEvaluationStrategy(handlers);
        
        var featureFlag = CreateFeatureFlagWithTargets("disabled-feature");
        featureFlag.IsEnabled = false; // Disabled at feature level
        
        var context = new FeatureContext { TenantId = Guid.NewGuid() };

        // Act
        var result = await strategy.EvaluateAsync(featureFlag, context);

        // Assert
        result.IsEnabled.Should().BeFalse("Disabled feature should always return false");
        result.Reason.Should().Contain("disabled");
        
        // Handler should not be called when feature is disabled
        handler.Verify(
            h => h.EvaluateAsync(It.IsAny<FeatureFlag>(), It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Handlers should not be called when feature is disabled");
    }

    [Fact]
    public async Task EvaluateAsync_HigherPriorityHandler_OverridesLowerPriority()
    {
        // Arrange - High priority returns disabled, low priority would return enabled
        var highPriorityHandler = CreateMockHandler(priority: 1, 
            returnValue: new FeatureEvaluationResult { IsEnabled = false, Reason = "High priority blocked" },
            new List<int>());
        
        var lowPriorityHandler = CreateMockHandler(priority: 2,
            returnValue: new FeatureEvaluationResult { IsEnabled = true, Reason = "Low priority allowed" },
            new List<int>());

        var handlers = new List<ITargetingRuleHandler>
        {
            lowPriorityHandler.Object, // Added first but lower priority
            highPriorityHandler.Object // Added second but higher priority
        };

        var strategy = new TargetedEvaluationStrategy(handlers);
        var featureFlag = CreateFeatureFlagWithTargets("priority-test");
        var context = new FeatureContext { TenantId = Guid.NewGuid() };

        // Act
        var result = await strategy.EvaluateAsync(featureFlag, context);

        // Assert
        result.IsEnabled.Should().BeFalse("Higher priority handler's result should be used");
        result.Reason.Should().Contain("High priority");
        
        // Low priority handler should not be called
        lowPriorityHandler.Verify(
            h => h.EvaluateAsync(It.IsAny<FeatureFlag>(), It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Lower priority handler should not be called if higher priority returns a result");
    }

    #region Helper Methods

    private static Mock<ITargetingRuleHandler> CreateMockHandler(
        int priority, 
        FeatureEvaluationResult? returnValue, 
        List<int> callOrder)
    {
        var mock = new Mock<ITargetingRuleHandler>();
        mock.Setup(h => h.Priority).Returns(priority);
        mock.Setup(h => h.EvaluateAsync(It.IsAny<FeatureFlag>(), It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add(priority))
            .ReturnsAsync(returnValue);

        return mock;
    }

    private static FeatureFlag CreateFeatureFlag(string key)
    {
        return new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = key,
            IsEnabled = true,
            Type = FeatureFlagType.UserSegment,
            DefaultValue = "false",
            EnabledValue = "true"
        };
    }

    private static FeatureFlag CreateFeatureFlagWithTargets(string key)
    {
        var featureFlag = CreateFeatureFlag(key);
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            Id = Guid.NewGuid(),
            FeatureFlagId = featureFlag.Id,
            TargetType = FeatureFlagConstants.TargetTypes.Tenant,
            TargetIdentifier = Guid.NewGuid().ToString(),
            IsEnabled = true,
            RolloutPercentage = 100
        });

        return featureFlag;
    }

    #endregion
}
