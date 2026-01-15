using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Features.UnitTests.Handlers;

/// <summary>
/// Tests for TenantTargetingHandler to verify fail-closed behavior.
/// </summary>
public class TenantTargetingHandlerTests
{
    private readonly Mock<ILogger<TenantTargetingHandler>> _loggerMock;
    private readonly TenantTargetingHandler _handler;

    public TenantTargetingHandlerTests()
    {
        _loggerMock = new Mock<ILogger<TenantTargetingHandler>>();
        _handler = new TenantTargetingHandler(_loggerMock.Object);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsNull_WhenNoTenantTargetingRules()
    {
        // Arrange
        var featureFlag = CreateFeatureFlag("test-feature", hasTargets: false);
        var context = new FeatureContext { TenantId = null };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().BeNull("No tenant targeting rules means handler should pass to next handler");
    }

    [Fact]
    public async Task EvaluateAsync_FailsClosed_WhenTenantTargetingExistsButNoTenantIdProvided()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var featureFlag = CreateFeatureFlag("premium-feature", hasTargets: true, targetTenantId: tenantId);
        var context = new FeatureContext { TenantId = null };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull("Fail-closed policy should be enforced");
        result!.IsEnabled.Should().BeFalse("Feature should be disabled when tenant targeting exists but no TenantId");
        result.Reason.Should().Contain("Fail-closed");
        result.TargetType.Should().Be(FeatureFlagConstants.TargetTypes.Tenant);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsEnabled_WhenTenantIsTargeted()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var featureFlag = CreateFeatureFlag("premium-feature", hasTargets: true, targetTenantId: tenantId);
        var context = new FeatureContext { TenantId = tenantId };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull();
        result!.IsEnabled.Should().BeTrue("Targeted tenant should have access");
        result.Reason.Should().Contain("targeted");
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsNull_WhenTenantNotInTargetList()
    {
        // Arrange
        var targetedTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var featureFlag = CreateFeatureFlag("premium-feature", hasTargets: true, targetTenantId: targetedTenantId);
        var context = new FeatureContext { TenantId = otherTenantId };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().BeNull("Non-targeted tenant should pass to other handlers");
    }

    [Fact]
    public async Task EvaluateAsync_NoCrossTenantLeakage_WhenMissingContext()
    {
        // Arrange - Feature flag with multiple tenant targets
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var featureFlag = CreateFeatureFlagWithMultipleTenants("enterprise-feature", tenant1, tenant2);
        var context = new FeatureContext { TenantId = null };

        // Act
        var result = await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        result.Should().NotBeNull("Must not leak to unknown tenant");
        result!.IsEnabled.Should().BeFalse("Must be disabled for security");
        result.Reason.Should().Contain("Fail-closed");
    }

    [Fact]
    public async Task EvaluateAsync_LogsWarning_WhenFailingClosed()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var featureFlag = CreateFeatureFlag("secure-feature", hasTargets: true, targetTenantId: tenantId);
        var context = new FeatureContext { TenantId = null };

        // Act
        await _handler.EvaluateAsync(featureFlag, context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fail-closed") || v.ToString()!.Contains("fail-closed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static FeatureFlag CreateFeatureFlag(string key, bool hasTargets, Guid? targetTenantId = null)
    {
        var featureFlag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = key,
            IsEnabled = true,
            Type = FeatureFlagType.UserSegment,
            DefaultValue = "false",
            EnabledValue = "true"
        };

        if (hasTargets && targetTenantId.HasValue)
        {
            featureFlag.Targets.Add(new FeatureFlagTarget
            {
                Id = Guid.NewGuid(),
                FeatureFlagId = featureFlag.Id,
                TargetType = FeatureFlagConstants.TargetTypes.Tenant,
                TargetIdentifier = targetTenantId.Value.ToString(),
                IsEnabled = true,
                RolloutPercentage = 100
            });
        }

        return featureFlag;
    }

    private static FeatureFlag CreateFeatureFlagWithMultipleTenants(string key, params Guid[] tenantIds)
    {
        var featureFlag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = key,
            IsEnabled = true,
            Type = FeatureFlagType.UserSegment,
            DefaultValue = "false",
            EnabledValue = "true"
        };

        foreach (var tenantId in tenantIds)
        {
            featureFlag.Targets.Add(new FeatureFlagTarget
            {
                Id = Guid.NewGuid(),
                FeatureFlagId = featureFlag.Id,
                TargetType = FeatureFlagConstants.TargetTypes.Tenant,
                TargetIdentifier = tenantId.ToString(),
                IsEnabled = true,
                RolloutPercentage = 100
            });
        }

        return featureFlag;
    }
}
