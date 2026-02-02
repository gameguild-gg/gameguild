using FluentAssertions;

using Xunit;

namespace GameGuild.Features.UnitTests.Strategies;

public class SimpleToggleStrategyTests
{
    private readonly SimpleToggleStrategy _strategy;

    public SimpleToggleStrategyTests()
    {
        _strategy = new SimpleToggleStrategy();
    }

    [Fact]
    public void FeatureType_ReturnsToggle()
    {
        // Act
        var result = _strategy.FeatureType;

        // Assert
        result.Should().Be(FeatureFlagType.Toggle);
    }

    [Fact]
    public async Task EvaluateAsync_EnabledFlag_ReturnsEnabledWithEnabledValue()
    {
        // Arrange
        var featureFlag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "test-toggle",
            Name = "Test Toggle",
            IsEnabled = true,
            Type = FeatureFlagType.Toggle,
            DefaultValue = "false",
            EnabledValue = "true"
        };
        var context = new FeatureContext();

        // Act
        var result = await _strategy.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result.IsEnabled.Should().BeTrue();
        result.Value.Should().Be("true");
        result.Reason.Should().Be("Feature is enabled");
    }

    [Fact]
    public async Task EvaluateAsync_DisabledFlag_ReturnsDisabledWithDefaultValue()
    {
        // Arrange
        var featureFlag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "test-toggle",
            Name = "Test Toggle",
            IsEnabled = false,
            Type = FeatureFlagType.Toggle,
            DefaultValue = "false",
            EnabledValue = "true"
        };
        var context = new FeatureContext();

        // Act
        var result = await _strategy.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result.IsEnabled.Should().BeFalse();
        result.Value.Should().Be("false");
        result.Reason.Should().Be("Feature is disabled");
    }

    [Fact]
    public async Task EvaluateAsync_CustomValues_ReturnsCorrectValue()
    {
        // Arrange
        var featureFlag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "custom-toggle",
            Name = "Custom Toggle",
            IsEnabled = true,
            Type = FeatureFlagType.Toggle,
            DefaultValue = "off",
            EnabledValue = "on"
        };
        var context = new FeatureContext();

        // Act
        var result = await _strategy.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be("on");
    }

    [Fact]
    public async Task EvaluateAsync_NullContext_StillWorks()
    {
        // Arrange
        var featureFlag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "test-toggle",
            Name = "Test Toggle",
            IsEnabled = true,
            Type = FeatureFlagType.Toggle,
            DefaultValue = "false",
            EnabledValue = "true"
        };
        var context = new FeatureContext();

        // Act
        var result = await _strategy.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WithCancellationToken_ReturnsResult()
    {
        // Arrange
        var featureFlag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "test-toggle",
            Name = "Test Toggle",
            IsEnabled = false,
            Type = FeatureFlagType.Toggle,
            DefaultValue = "false",
            EnabledValue = "true"
        };
        var context = new FeatureContext();
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _strategy.EvaluateAsync(featureFlag, context, cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task EvaluateAsync_MultipleEvaluations_ConsistentResults()
    {
        // Arrange
        var featureFlag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "test-toggle",
            Name = "Test Toggle",
            IsEnabled = true,
            Type = FeatureFlagType.Toggle,
            DefaultValue = "false",
            EnabledValue = "true"
        };
        var context = new FeatureContext();

        // Act
        var result1 = await _strategy.EvaluateAsync(featureFlag, context);
        var result2 = await _strategy.EvaluateAsync(featureFlag, context);

        // Assert
        result1.IsEnabled.Should().Be(result2.IsEnabled);
        result1.Value.Should().Be(result2.Value);
        result1.Reason.Should().Be(result2.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_TenantContext_IgnoresContext()
    {
        // Arrange - SimpleToggleStrategy doesn't use context
        var featureFlag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "test-toggle",
            Name = "Test Toggle",
            IsEnabled = true,
            Type = FeatureFlagType.Toggle,
            DefaultValue = "false",
            EnabledValue = "true"
        };
        var contextWithTenant = new FeatureContext { TenantId = Guid.NewGuid() };
        var contextWithoutTenant = new FeatureContext();

        // Act
        var resultWithTenant = await _strategy.EvaluateAsync(featureFlag, contextWithTenant);
        var resultWithoutTenant = await _strategy.EvaluateAsync(featureFlag, contextWithoutTenant);

        // Assert - Results should be identical
        resultWithTenant.IsEnabled.Should().Be(resultWithoutTenant.IsEnabled);
        resultWithTenant.Value.Should().Be(resultWithoutTenant.Value);
    }

    [Fact]
    public async Task EvaluateAsync_UserContext_IgnoresContext()
    {
        // Arrange - SimpleToggleStrategy doesn't use context
        var featureFlag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = "test-toggle",
            Name = "Test Toggle",
            IsEnabled = false,
            Type = FeatureFlagType.Toggle,
            DefaultValue = "false",
            EnabledValue = "true"
        };
        var contextWithUser = new FeatureContext { UserId = Guid.NewGuid() };
        var contextWithoutUser = new FeatureContext();

        // Act
        var resultWithUser = await _strategy.EvaluateAsync(featureFlag, contextWithUser);
        var resultWithoutUser = await _strategy.EvaluateAsync(featureFlag, contextWithoutUser);

        // Assert - Results should be identical
        resultWithUser.IsEnabled.Should().Be(resultWithoutUser.IsEnabled);
        resultWithUser.Value.Should().Be(resultWithoutUser.Value);
    }
}
