using FluentAssertions;
using GameGuild.Features;
using Xunit;

namespace GameGuild.Tests.Features.Unit.Handlers;

/// <summary>
/// Unit tests for UserTargetingHandler
/// </summary>
public class UserTargetingHandlerTests
{
    private readonly UserTargetingHandler _handler;

    public UserTargetingHandlerTests()
    {
        _handler = new UserTargetingHandler();
    }

    [Fact]
    public void Priority_ShouldBe2()
    {
        _handler.Priority.Should().Be(2);
    }

    [Fact]
    public async Task EvaluateAsync_WhenUserIdIsNull_ReturnsNull()
    {
        // Arrange
        var featureFlag = CreateFeatureFlag("test-feature");
        var context = new FeatureContext { TenantId = Guid.NewGuid(), UserId = null };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_WhenNoUserTarget_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var featureFlag = CreateFeatureFlag("test-feature");
        var context = new FeatureContext { TenantId = Guid.NewGuid(), UserId = userId };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_WhenUserIsTargetedAndEnabled_ReturnsEnabled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.User,
            TargetIdentifier = userId.ToString(),
            IsEnabled = true,
            RolloutPercentage = 100
        });
        var context = new FeatureContext { TenantId = Guid.NewGuid(), UserId = userId };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result!.IsEnabled.Should().BeTrue();
        result.IsTargeted.Should().BeTrue();
        result.TargetType.Should().Be(FeatureFlagConstants.TargetTypes.User);
        result.Reason.Should().Contain(userId.ToString());
    }

    [Fact]
    public async Task EvaluateAsync_WhenUserIsTargetedAndDisabled_ReturnsDisabled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.User,
            TargetIdentifier = userId.ToString(),
            IsEnabled = false,
            RolloutPercentage = 100
        });
        var context = new FeatureContext { TenantId = Guid.NewGuid(), UserId = userId };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result!.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_WhenUserHasCustomValue_ReturnsCustomValue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string customValue = "custom-value-for-user";
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.User,
            TargetIdentifier = userId.ToString(),
            IsEnabled = true,
            RolloutPercentage = 100,
            CustomValue = customValue
        });
        var context = new FeatureContext { TenantId = Guid.NewGuid(), UserId = userId };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(customValue);
    }

    [Fact]
    public async Task EvaluateAsync_WhenRolloutPercentageIsLessThan100_AppliesRollout()
    {
        // Arrange - Test deterministic rollout behavior
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.User,
            TargetIdentifier = userId.ToString(),
            IsEnabled = true,
            RolloutPercentage = 50 // 50% rollout
        });
        var context = new FeatureContext { TenantId = tenantId, UserId = userId };

        // Act - Run multiple times to verify determinism
        var results = new List<bool>();
        for (int i = 0; i < 10; i++)
        {
            var result = await _handler.EvaluateAsync(featureFlag, context);
            results.Add(result!.IsEnabled);
        }

        // Assert - Should always return the same value (deterministic)
        results.Distinct().Should().HaveCount(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(99)]
    [InlineData(100)]
    public async Task EvaluateAsync_IncludesRolloutPercentageInResult(int rolloutPercentage)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.User,
            TargetIdentifier = userId.ToString(),
            IsEnabled = true,
            RolloutPercentage = rolloutPercentage
        });
        var context = new FeatureContext { TenantId = Guid.NewGuid(), UserId = userId };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result!.RolloutPercentage.Should().Be(rolloutPercentage);
    }

    [Fact]
    public async Task EvaluateAsync_SetsEvaluatedAtToCurrentTime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.User,
            TargetIdentifier = userId.ToString(),
            IsEnabled = true,
            RolloutPercentage = 100
        });
        var context = new FeatureContext { TenantId = Guid.NewGuid(), UserId = userId };
        var before = DateTime.UtcNow;

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);
        var after = DateTime.UtcNow;

        // Assert
        result.Should().NotBeNull();
        result!.EvaluatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task EvaluateAsync_WhenDisabled_ReturnsDefaultValue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string defaultValue = "default-value";
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.DefaultValue = defaultValue;
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.User,
            TargetIdentifier = userId.ToString(),
            IsEnabled = false,
            RolloutPercentage = 100
        });
        var context = new FeatureContext { TenantId = Guid.NewGuid(), UserId = userId };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(defaultValue);
    }

    private static FeatureFlag CreateFeatureFlag(string key) => new FeatureFlag
    {
        Key = key,
        Name = key,
        IsEnabled = true,
        EnabledValue = "enabled-value",
        DefaultValue = "default-value",
        Targets = new List<FeatureFlagTarget>()
    };
}

/// <summary>
/// Unit tests for CountryTargetingHandler
/// </summary>
public class CountryTargetingHandlerTests
{
    private readonly CountryTargetingHandler _handler;

    public CountryTargetingHandlerTests()
    {
        _handler = new CountryTargetingHandler();
    }

    [Fact]
    public void Priority_ShouldBe4()
    {
        _handler.Priority.Should().Be(4);
    }

    [Fact]
    public async Task EvaluateAsync_WhenCountryIsNull_ReturnsNull()
    {
        // Arrange
        var featureFlag = CreateFeatureFlag("test-feature");
        var context = new FeatureContext { TenantId = Guid.NewGuid(), Country = null };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_WhenNoCountryTarget_ReturnsNull()
    {
        // Arrange
        var featureFlag = CreateFeatureFlag("test-feature");
        var context = new FeatureContext { TenantId = Guid.NewGuid(), Country = "US" };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_WhenCountryIsTargetedAndEnabled_ReturnsEnabled()
    {
        // Arrange
        const string country = "US";
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.Country,
            TargetIdentifier = country,
            IsEnabled = true,
            RolloutPercentage = 100
        });
        var context = new FeatureContext { TenantId = Guid.NewGuid(), Country = country };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result!.IsEnabled.Should().BeTrue();
        result.IsTargeted.Should().BeTrue();
        result.TargetType.Should().Be(FeatureFlagConstants.TargetTypes.Country);
        result.Reason.Should().Contain(country);
    }

    [Theory]
    [InlineData("us", "US")] // Case insensitive
    [InlineData("US", "us")]
    [InlineData("Gb", "GB")]
    public async Task EvaluateAsync_MatchesCountryCaseInsensitively(string targetCountry, string contextCountry)
    {
        // Arrange
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.Country,
            TargetIdentifier = targetCountry,
            IsEnabled = true,
            RolloutPercentage = 100
        });
        var context = new FeatureContext { TenantId = Guid.NewGuid(), Country = contextCountry };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result!.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WhenCountryIsTargetedAndDisabled_ReturnsDisabled()
    {
        // Arrange
        const string country = "BR";
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.Country,
            TargetIdentifier = country,
            IsEnabled = false,
            RolloutPercentage = 100
        });
        var context = new FeatureContext { TenantId = Guid.NewGuid(), Country = country };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result!.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_WithPartialRollout_AppliesRolloutConsistently()
    {
        // Arrange
        const string country = "JP";
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.Country,
            TargetIdentifier = country,
            IsEnabled = true,
            RolloutPercentage = 50
        });
        var context = new FeatureContext { TenantId = tenantId, UserId = userId, Country = country };

        // Act - Run multiple times to verify determinism
        var results = new List<bool>();
        for (int i = 0; i < 10; i++)
        {
            var result = await _handler.EvaluateAsync(featureFlag, context);
            results.Add(result!.IsEnabled);
        }

        // Assert - Should always return the same value (deterministic)
        results.Distinct().Should().HaveCount(1);
    }

    [Fact]
    public async Task EvaluateAsync_WhenUserIdIsNull_StillAppliesRollout()
    {
        // Arrange
        const string country = "DE";
        var tenantId = Guid.NewGuid();
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.Country,
            TargetIdentifier = country,
            IsEnabled = true,
            RolloutPercentage = 50
        });
        var context = new FeatureContext { TenantId = tenantId, UserId = null, Country = country };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert - Should not throw
        result.Should().NotBeNull();
        result!.RolloutPercentage.Should().Be(50);
    }

    private static FeatureFlag CreateFeatureFlag(string key) => new FeatureFlag
    {
        Key = key,
        Name = key,
        IsEnabled = true,
        EnabledValue = "enabled-value",
        DefaultValue = "default-value",
        Targets = new List<FeatureFlagTarget>()
    };
}

/// <summary>
/// Unit tests for PlanTargetingHandler
/// </summary>
public class PlanTargetingHandlerTests
{
    private readonly PlanTargetingHandler _handler;

    public PlanTargetingHandlerTests()
    {
        _handler = new PlanTargetingHandler();
    }

    [Fact]
    public void Priority_ShouldBe3()
    {
        _handler.Priority.Should().Be(3);
    }

    [Fact]
    public async Task EvaluateAsync_WhenPlanIsNull_ReturnsNull()
    {
        // Arrange
        var featureFlag = CreateFeatureFlag("test-feature");
        var context = new FeatureContext { TenantId = Guid.NewGuid(), SubscriptionPlanId = null };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_WhenNoPlanTarget_ReturnsNull()
    {
        // Arrange
        var featureFlag = CreateFeatureFlag("test-feature");
        var context = new FeatureContext { TenantId = Guid.NewGuid(), SubscriptionPlanId = "pro" };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_WhenPlanIsTargetedAndEnabled_ReturnsEnabled()
    {
        // Arrange
        const string plan = "enterprise";
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.Plan,
            TargetIdentifier = plan,
            IsEnabled = true,
            RolloutPercentage = 100
        });
        var context = new FeatureContext { TenantId = Guid.NewGuid(), SubscriptionPlanId = plan };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result!.IsEnabled.Should().BeTrue();
        result.IsTargeted.Should().BeTrue();
        result.TargetType.Should().Be(FeatureFlagConstants.TargetTypes.Plan);
    }

    [Theory]
    [InlineData("free")]
    [InlineData("starter")]
    [InlineData("pro")]
    [InlineData("enterprise")]
    public async Task EvaluateAsync_WorksWithDifferentPlans(string plan)
    {
        // Arrange
        var featureFlag = CreateFeatureFlag("test-feature");
        featureFlag.Targets.Add(new FeatureFlagTarget
        {
            TargetType = FeatureFlagConstants.TargetTypes.Plan,
            TargetIdentifier = plan,
            IsEnabled = true,
            RolloutPercentage = 100
        });
        var context = new FeatureContext { TenantId = Guid.NewGuid(), SubscriptionPlanId = plan };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result!.IsEnabled.Should().BeTrue();
    }

    private static FeatureFlag CreateFeatureFlag(string key) => new FeatureFlag
    {
        Key = key,
        Name = key,
        IsEnabled = true,
        EnabledValue = "enabled-value",
        DefaultValue = "default-value",
        Targets = new List<FeatureFlagTarget>()
    };
}
