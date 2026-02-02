using FluentAssertions;
using Xunit;

namespace GameGuild.Features.UnitTests.Entities;

/// <summary>
/// Unit tests for TenantCapability entity
/// </summary>
public class TenantCapabilityTests
{
    #region Constructor Tests

    [Fact]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var capability = new TenantCapability();

        // Assert
        capability.CapabilityKey.Should().BeEmpty();
        capability.IsEnabled.Should().BeFalse();
        capability.Priority.Should().Be(0);
        capability.Source.Should().BeNull();
        capability.ExpiresAt.Should().BeNull();
        capability.Metadata.Should().BeNull();
        capability.ModifiedByUserId.Should().BeNull();
        capability.ModificationReason.Should().BeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void TenantId_ShouldBeSettable()
    {
        // Arrange
        var capability = new TenantCapability();
        var tenantId = Guid.NewGuid();

        // Act
        capability.TenantId = tenantId;

        // Assert
        capability.TenantId.Should().Be(tenantId);
    }

    [Theory]
    [InlineData("lxp.discovery")]
    [InlineData("lxp.learningPaths")]
    [InlineData("lms.certificates")]
    [InlineData("api.rateLimit.premium")]
    public void CapabilityKey_ShouldAcceptValidKeys(string key)
    {
        // Arrange
        var capability = new TenantCapability();

        // Act
        capability.CapabilityKey = key;

        // Assert
        capability.CapabilityKey.Should().Be(key);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsEnabled_ShouldAcceptBoolValues(bool isEnabled)
    {
        // Arrange
        var capability = new TenantCapability();

        // Act
        capability.IsEnabled = isEnabled;

        // Assert
        capability.IsEnabled.Should().Be(isEnabled);
    }

    [Theory]
    [InlineData("plan:free")]
    [InlineData("plan:pro")]
    [InlineData("override:admin")]
    [InlineData("trial")]
    [InlineData("promotional")]
    public void Source_ShouldAcceptValidSources(string source)
    {
        // Arrange
        var capability = new TenantCapability();

        // Act
        capability.Source = source;

        // Assert
        capability.Source.Should().Be(source);
    }

    [Fact]
    public void ExpiresAt_ShouldBeSettable()
    {
        // Arrange
        var capability = new TenantCapability();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);

        // Act
        capability.ExpiresAt = expiresAt;

        // Assert
        capability.ExpiresAt.Should().Be(expiresAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public void Priority_ShouldAcceptValidPriorities(int priority)
    {
        // Arrange
        var capability = new TenantCapability();

        // Act
        capability.Priority = priority;

        // Assert
        capability.Priority.Should().Be(priority);
    }

    [Fact]
    public void Metadata_ShouldAcceptJsonString()
    {
        // Arrange
        var capability = new TenantCapability();
        var metadata = "{\"maxUsers\": 100, \"features\": [\"a\", \"b\"]}";

        // Act
        capability.Metadata = metadata;

        // Assert
        capability.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void ModifiedByUserId_ShouldBeSettable()
    {
        // Arrange
        var capability = new TenantCapability();
        var userId = Guid.NewGuid();

        // Act
        capability.ModifiedByUserId = userId;

        // Assert
        capability.ModifiedByUserId.Should().Be(userId);
    }

    [Fact]
    public void ModificationReason_ShouldBeSettable()
    {
        // Arrange
        var capability = new TenantCapability();

        // Act
        capability.ModificationReason = "Upgraded to pro plan";

        // Assert
        capability.ModificationReason.Should().Be("Upgraded to pro plan");
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void TenantCapability_ForProPlan_ShouldHaveCorrectConfiguration()
    {
        // Arrange & Act
        var capability = new TenantCapability
        {
            TenantId = Guid.NewGuid(),
            CapabilityKey = "lxp.advancedAnalytics",
            IsEnabled = true,
            Source = "plan:pro",
            Priority = 100,
            Metadata = "{\"refreshIntervalMinutes\": 15}"
        };

        // Assert
        capability.IsEnabled.Should().BeTrue();
        capability.Source.Should().Be("plan:pro");
        capability.Priority.Should().Be(100);
    }

    [Fact]
    public void TenantCapability_ForAdminOverride_ShouldHaveHighPriority()
    {
        // Arrange & Act
        var capability = new TenantCapability
        {
            TenantId = Guid.NewGuid(),
            CapabilityKey = "lms.unlimitedStorage",
            IsEnabled = true,
            Source = "override:admin",
            Priority = 1000,
            ModifiedByUserId = Guid.NewGuid(),
            ModificationReason = "Special enterprise customer"
        };

        // Assert
        capability.Priority.Should().Be(1000);
        capability.Source.Should().Be("override:admin");
        capability.ModificationReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TenantCapability_ForTrial_ShouldHaveExpiration()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddDays(14);

        // Act
        var capability = new TenantCapability
        {
            TenantId = Guid.NewGuid(),
            CapabilityKey = "premium.allFeatures",
            IsEnabled = true,
            Source = "trial",
            ExpiresAt = expiresAt,
            Priority = 50
        };

        // Assert
        capability.ExpiresAt.Should().Be(expiresAt);
        capability.Source.Should().Be("trial");
    }

    #endregion
}

/// <summary>
/// Unit tests for FeatureFlagTarget entity
/// </summary>
public class FeatureFlagTargetTests
{
    #region Constructor Tests

    [Fact]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var target = new FeatureFlagTarget();

        // Assert
        target.TargetType.Should().BeEmpty();
        target.TargetIdentifier.Should().BeEmpty();
        target.IsEnabled.Should().BeFalse();
        target.RolloutPercentage.Should().Be(100);
        target.Priority.Should().Be(0);
        target.CustomValue.Should().BeNull();
        target.Metadata.Should().BeNull();
        target.DependsOn.Should().BeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void FeatureFlagId_ShouldBeSettable()
    {
        // Arrange
        var target = new FeatureFlagTarget();
        var flagId = Guid.NewGuid();

        // Act
        target.FeatureFlagId = flagId;

        // Assert
        target.FeatureFlagId.Should().Be(flagId);
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("user")]
    [InlineData("plan")]
    [InlineData("environment")]
    [InlineData("group")]
    public void TargetType_ShouldAcceptValidTypes(string targetType)
    {
        // Arrange
        var target = new FeatureFlagTarget();

        // Act
        target.TargetType = targetType;

        // Assert
        target.TargetType.Should().Be(targetType);
    }

    [Fact]
    public void TargetIdentifier_ShouldAcceptGuidString()
    {
        // Arrange
        var target = new FeatureFlagTarget();
        var identifier = Guid.NewGuid().ToString();

        // Act
        target.TargetIdentifier = identifier;

        // Assert
        target.TargetIdentifier.Should().Be(identifier);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsEnabled_ShouldAcceptBoolValues(bool isEnabled)
    {
        // Arrange
        var target = new FeatureFlagTarget();

        // Act
        target.IsEnabled = isEnabled;

        // Assert
        target.IsEnabled.Should().Be(isEnabled);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(100)]
    public void RolloutPercentage_ShouldAcceptValidRange(int percentage)
    {
        // Arrange
        var target = new FeatureFlagTarget();

        // Act
        target.RolloutPercentage = percentage;

        // Assert
        target.RolloutPercentage.Should().Be(percentage);
    }

    [Fact]
    public void CustomValue_ShouldBeSettable()
    {
        // Arrange
        var target = new FeatureFlagTarget();

        // Act
        target.CustomValue = "special_value";

        // Assert
        target.CustomValue.Should().Be("special_value");
    }

    [Fact]
    public void Metadata_ShouldAcceptJsonString()
    {
        // Arrange
        var target = new FeatureFlagTarget();
        var metadata = "{\"conditions\": [{\"field\": \"country\", \"value\": \"US\"}]}";

        // Act
        target.Metadata = metadata;

        // Assert
        target.Metadata.Should().Be(metadata);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Priority_ShouldAcceptValidValues(int priority)
    {
        // Arrange
        var target = new FeatureFlagTarget();

        // Act
        target.Priority = priority;

        // Assert
        target.Priority.Should().Be(priority);
    }

    [Fact]
    public void DependsOn_ShouldBeSettable()
    {
        // Arrange
        var target = new FeatureFlagTarget();

        // Act
        target.DependsOn = "parent-feature-flag";

        // Assert
        target.DependsOn.Should().Be("parent-feature-flag");
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void FeatureFlagTarget_ForTenantOverride_ShouldBeConfigured()
    {
        // Arrange & Act
        var target = new FeatureFlagTarget
        {
            FeatureFlagId = Guid.NewGuid(),
            TargetType = "tenant",
            TargetIdentifier = Guid.NewGuid().ToString(),
            IsEnabled = true,
            RolloutPercentage = 100,
            Priority = 500
        };

        // Assert
        target.TargetType.Should().Be("tenant");
        target.IsEnabled.Should().BeTrue();
        target.RolloutPercentage.Should().Be(100);
    }

    [Fact]
    public void FeatureFlagTarget_ForGradualRollout_ShouldHavePercentage()
    {
        // Arrange & Act
        var target = new FeatureFlagTarget
        {
            FeatureFlagId = Guid.NewGuid(),
            TargetType = "user",
            TargetIdentifier = "*",
            IsEnabled = true,
            RolloutPercentage = 25,
            Priority = 100
        };

        // Assert
        target.RolloutPercentage.Should().Be(25);
    }

    #endregion
}

/// <summary>
/// Unit tests for FeatureFlagUsage entity
/// </summary>
public class FeatureFlagUsageTests
{
    #region Constructor Tests

    [Fact]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var usage = new FeatureFlagUsage();

        // Assert
        usage.Environment.Should().Be("production");
        usage.AccessCount.Should().Be(1);
        usage.WasEnabled.Should().BeFalse();
        usage.TenantId.Should().BeNull();
        usage.UserId.Should().BeNull();
        usage.ReturnedValue.Should().BeNull();
        usage.ContextData.Should().BeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void FeatureFlagId_ShouldBeSettable()
    {
        // Arrange
        var usage = new FeatureFlagUsage();
        var flagId = Guid.NewGuid();

        // Act
        usage.FeatureFlagId = flagId;

        // Assert
        usage.FeatureFlagId.Should().Be(flagId);
    }

    [Fact]
    public void TenantId_ShouldBeSettable()
    {
        // Arrange
        var usage = new FeatureFlagUsage();
        var tenantId = Guid.NewGuid();

        // Act
        usage.TenantId = tenantId;

        // Assert
        usage.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void UserId_ShouldBeSettable()
    {
        // Arrange
        var usage = new FeatureFlagUsage();
        var userId = Guid.NewGuid();

        // Act
        usage.UserId = userId;

        // Assert
        usage.UserId.Should().Be(userId);
    }

    [Theory]
    [InlineData("development")]
    [InlineData("staging")]
    [InlineData("production")]
    public void Environment_ShouldAcceptValidValues(string environment)
    {
        // Arrange
        var usage = new FeatureFlagUsage();

        // Act
        usage.Environment = environment;

        // Assert
        usage.Environment.Should().Be(environment);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(10000)]
    [InlineData(long.MaxValue)]
    public void AccessCount_ShouldAcceptValidValues(long count)
    {
        // Arrange
        var usage = new FeatureFlagUsage();

        // Act
        usage.AccessCount = count;

        // Assert
        usage.AccessCount.Should().Be(count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WasEnabled_ShouldAcceptBoolValues(bool wasEnabled)
    {
        // Arrange
        var usage = new FeatureFlagUsage();

        // Act
        usage.WasEnabled = wasEnabled;

        // Assert
        usage.WasEnabled.Should().Be(wasEnabled);
    }

    [Fact]
    public void ReturnedValue_ShouldBeSettable()
    {
        // Arrange
        var usage = new FeatureFlagUsage();

        // Act
        usage.ReturnedValue = "variant_b";

        // Assert
        usage.ReturnedValue.Should().Be("variant_b");
    }

    [Fact]
    public void FirstAccessAt_ShouldBeSettable()
    {
        // Arrange
        var usage = new FeatureFlagUsage();
        var accessTime = DateTime.UtcNow.AddHours(-1);

        // Act
        usage.FirstAccessAt = accessTime;

        // Assert
        usage.FirstAccessAt.Should().Be(accessTime);
    }

    [Fact]
    public void LastAccessAt_ShouldBeSettable()
    {
        // Arrange
        var usage = new FeatureFlagUsage();
        var accessTime = DateTime.UtcNow;

        // Act
        usage.LastAccessAt = accessTime;

        // Assert
        usage.LastAccessAt.Should().Be(accessTime);
    }

    [Fact]
    public void ContextData_ShouldAcceptJsonString()
    {
        // Arrange
        var usage = new FeatureFlagUsage();
        var contextData = "{\"browser\": \"Chrome\", \"version\": \"120\"}";

        // Act
        usage.ContextData = contextData;

        // Assert
        usage.ContextData.Should().Be(contextData);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void FeatureFlagUsage_ForTracking_ShouldHaveCompleteData()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var usage = new FeatureFlagUsage
        {
            FeatureFlagId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Environment = "production",
            AccessCount = 150,
            WasEnabled = true,
            ReturnedValue = "new_ui",
            FirstAccessAt = now.AddDays(-7),
            LastAccessAt = now,
            ContextData = "{\"feature\": \"dashboard\"}"
        };

        // Assert
        usage.AccessCount.Should().Be(150);
        usage.WasEnabled.Should().BeTrue();
        usage.FirstAccessAt.Should().BeBefore(usage.LastAccessAt);
    }

    #endregion
}
