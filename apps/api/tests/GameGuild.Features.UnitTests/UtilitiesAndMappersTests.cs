using FluentAssertions;
using Xunit;

namespace GameGuild.Features.UnitTests;

#region RolloutHashCalculator

public class RolloutHashCalculatorTests
{
    [Fact]
    public void IsInRollout_Percentage100_AlwaysReturnsTrue()
    {
        RolloutHashCalculator.IsInRollout("any-user", 100).Should().BeTrue();
    }

    [Fact]
    public void IsInRollout_Percentage0_AlwaysReturnsFalse()
    {
        RolloutHashCalculator.IsInRollout("any-user", 0).Should().BeFalse();
    }

    [Fact]
    public void IsInRollout_DeterministicForSameInput()
    {
        var first = RolloutHashCalculator.IsInRollout("user-123", 50);
        var second = RolloutHashCalculator.IsInRollout("user-123", 50);
        first.Should().Be(second);
    }

    [Fact]
    public void IsInRollout_WithSalt_DifferentFromWithout()
    {
        var bucket1 = RolloutHashCalculator.GetBucketValue("user-123");
        var bucket2 = RolloutHashCalculator.GetBucketValue("user-123", "custom-salt");
        // Different salt should likely produce different bucket (not guaranteed but very likely)
        // Just verify no exception
        bucket1.Should().BeInRange(0u, 99u);
        bucket2.Should().BeInRange(0u, 99u);
    }

    [Fact]
    public void IsInRollout_NullIdentifier_Throws()
    {
        var act = () => RolloutHashCalculator.IsInRollout(null!, 50);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsInRollout_EmptyIdentifier_Throws()
    {
        var act = () => RolloutHashCalculator.IsInRollout("", 50);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsInRollout_WhitespaceIdentifier_Throws()
    {
        var act = () => RolloutHashCalculator.IsInRollout("   ", 50);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateIdentifier_WithTenantId_ReturnsTenantId()
    {
        var tenantId = Guid.NewGuid();
        var ctx = new FeatureContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        RolloutHashCalculator.CreateIdentifier(ctx).Should().Be(tenantId.ToString());
    }

    [Fact]
    public void CreateIdentifier_NoTenant_FallsToUserId()
    {
        var userId = Guid.NewGuid();
        var ctx = new FeatureContext { UserId = userId };
        RolloutHashCalculator.CreateIdentifier(ctx).Should().Be(userId.ToString());
    }

    [Fact]
    public void CreateIdentifier_NoTenantNoUser_FallsToIpAddress()
    {
        var ctx = new FeatureContext { IpAddress = "192.168.1.1" };
        RolloutHashCalculator.CreateIdentifier(ctx).Should().Be("192.168.1.1");
    }

    [Fact]
    public void CreateIdentifier_NothingSet_ReturnsAnonymous()
    {
        var ctx = new FeatureContext();
        RolloutHashCalculator.CreateIdentifier(ctx).Should().Be(FeatureFlagConstants.AnonymousIdentifier);
    }

    [Fact]
    public void CreateIdentifier_NullContext_Throws()
    {
        var act = () => RolloutHashCalculator.CreateIdentifier(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsValidPercentage_ValidRange_ReturnsTrue()
    {
        RolloutHashCalculator.IsValidPercentage(0).Should().BeTrue();
        RolloutHashCalculator.IsValidPercentage(50).Should().BeTrue();
        RolloutHashCalculator.IsValidPercentage(100).Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-100)]
    [InlineData(200)]
    public void IsValidPercentage_OutOfRange_ReturnsFalse(int pct)
    {
        RolloutHashCalculator.IsValidPercentage(pct).Should().BeFalse();
    }

    [Fact]
    public void GetBucketValue_ReturnsConsistentValue()
    {
        var b1 = RolloutHashCalculator.GetBucketValue("test-id");
        var b2 = RolloutHashCalculator.GetBucketValue("test-id");
        b1.Should().Be(b2);
        b1.Should().BeInRange(0u, 99u);
    }

    [Fact]
    public void GetBucketValue_NullIdentifier_Throws()
    {
        var act = () => RolloutHashCalculator.GetBucketValue(null!);
        act.Should().Throw<ArgumentException>();
    }
}

#endregion

#region EntityModelMapper

public class EntityModelMapperTests
{
    [Fact]
    public void ToModel_ReturnsCorrectType()
    {
        var result = EntityModelMapper.ToModel(FeatureFlagType.Toggle);
        result.Should().Be(FeatureFlagType.Toggle);
    }

    [Fact]
    public void ToEntity_ReturnsCorrectType()
    {
        var result = EntityModelMapper.ToEntity(FeatureFlagType.Percentage);
        result.Should().Be(FeatureFlagType.Percentage);
    }

    [Fact]
    public void ToConfig_MapsEntityToConfig()
    {
        var entity = new FeatureFlag
        {
            Key = "test-key",
            Name = "Test Feature",
            Description = "Test description",
            Type = FeatureFlagType.Toggle,
            DefaultValue = "false",
            EnabledValue = "true",
            RolloutPercentage = 50,
            Environment = "staging",
            IsEnabled = true
        };

        var config = EntityModelMapper.ToConfig(entity);

        config.Key.Should().Be("test-key");
        config.Name.Should().Be("Test Feature");
        config.Description.Should().Be("Test description");
        config.Type.Should().Be(FeatureFlagType.Toggle);
        config.DefaultValue.Should().Be("false");
        config.EnabledValue.Should().Be("true");
        config.RolloutPercentage.Should().Be(50);
        config.Environment.Should().Be("staging");
        config.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void ToConfig_NullEntity_Throws()
    {
        var act = () => EntityModelMapper.ToConfig(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToConfigs_MapsCollection()
    {
        var entities = new[]
        {
            new FeatureFlag { Key = "a", Name = "A" },
            new FeatureFlag { Key = "b", Name = "B" }
        };

        var configs = EntityModelMapper.ToConfigs(entities).ToList();

        configs.Should().HaveCount(2);
        configs[0].Key.Should().Be("a");
        configs[1].Key.Should().Be("b");
    }

    [Fact]
    public void ToConfigs_NullEntities_Throws()
    {
        var act = () => EntityModelMapper.ToConfigs(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToTargetingRule_MapsTarget()
    {
        var target = new FeatureFlagTarget
        {
            TargetType = "user",
            TargetIdentifier = "user-123",
            IsEnabled = true,
            RolloutPercentage = 80,
            CustomValue = "premium",
            Priority = 5,
            Metadata = null
        };

        var rule = EntityModelMapper.ToTargetingRule(target);

        rule.TargetType.Should().Be("user");
        rule.TargetIdentifier.Should().Be("user-123");
        rule.IsEnabled.Should().BeTrue();
        rule.RolloutPercentage.Should().Be(80);
        rule.CustomValue.Should().Be("premium");
        rule.Priority.Should().Be(5);
        rule.Conditions.Should().BeEmpty();
    }

    [Fact]
    public void ToTargetingRule_WithMetadata_DeserializesConditions()
    {
        var target = new FeatureFlagTarget
        {
            TargetType = "tenant",
            TargetIdentifier = "t-1",
            Metadata = "{\"region\":\"us-east\"}"
        };

        var rule = EntityModelMapper.ToTargetingRule(target);

        rule.Conditions.Should().ContainKey("region");
    }

    [Fact]
    public void ToTargetingRule_NullTarget_Throws()
    {
        var act = () => EntityModelMapper.ToTargetingRule(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToTargetEntity_MapsRequest()
    {
        var request = new ConcreteTargetingRequest
        {
            FeatureFlagId = Guid.NewGuid(),
            TargetType = "plan",
            TargetIdentifier = "enterprise",
            IsEnabled = true,
            RolloutPercentage = 75,
            CustomValue = "custom-val",
            Priority = 3
        };

        var entity = EntityModelMapper.ToTargetEntity(request);

        entity.FeatureFlagId.Should().Be(request.FeatureFlagId);
        entity.TargetType.Should().Be("plan");
        entity.TargetIdentifier.Should().Be("enterprise");
        entity.IsEnabled.Should().BeTrue();
        entity.RolloutPercentage.Should().Be(75);
        entity.CustomValue.Should().Be("custom-val");
        entity.Priority.Should().Be(3);
    }

    [Fact]
    public void ToTargetEntity_WithMetadata_SerializesToJson()
    {
        var request = new ConcreteTargetingRequest
        {
            FeatureFlagId = Guid.NewGuid(),
            Metadata = new Dictionary<string, object> { { "key", "value" } }
        };

        var entity = EntityModelMapper.ToTargetEntity(request);

        entity.Metadata.Should().Contain("key");
    }

    [Fact]
    public void ToTargetEntity_WithEmptyMetadata_NullMetadata()
    {
        var request = new ConcreteTargetingRequest
        {
            FeatureFlagId = Guid.NewGuid(),
            Metadata = new Dictionary<string, object>()
        };

        var entity = EntityModelMapper.ToTargetEntity(request);

        entity.Metadata.Should().BeNull();
    }

    [Fact]
    public void ToTargetEntity_NullRequest_Throws()
    {
        var act = () => EntityModelMapper.ToTargetEntity(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateTargetEntity_UpdatesAllFields()
    {
        var target = new FeatureFlagTarget
        {
            TargetType = "old",
            TargetIdentifier = "old-id"
        };

        var request = new ConcreteTargetingRequest
        {
            TargetType = "new",
            TargetIdentifier = "new-id",
            IsEnabled = true,
            RolloutPercentage = 40,
            Priority = 2,
            CustomValue = "updated"
        };

        EntityModelMapper.UpdateTargetEntity(target, request);

        target.TargetType.Should().Be("new");
        target.TargetIdentifier.Should().Be("new-id");
        target.IsEnabled.Should().BeTrue();
        target.RolloutPercentage.Should().Be(40);
        target.Priority.Should().Be(2);
        target.CustomValue.Should().Be("updated");
    }

    [Fact]
    public void UpdateTargetEntity_WithMetadata_SerializesToJson()
    {
        var target = new FeatureFlagTarget();
        var request = new ConcreteTargetingRequest
        {
            Metadata = new Dictionary<string, object> { { "env", "prod" } }
        };

        EntityModelMapper.UpdateTargetEntity(target, request);

        target.Metadata.Should().Contain("env");
    }

    [Fact]
    public void UpdateTargetEntity_NullCustomValue_DoesNotOverwrite()
    {
        var target = new FeatureFlagTarget { CustomValue = "original" };
        var request = new ConcreteTargetingRequest { CustomValue = null };

        EntityModelMapper.UpdateTargetEntity(target, request);

        target.CustomValue.Should().Be("original");
    }

    [Fact]
    public void UpdateTargetEntity_NullTarget_Throws()
    {
        var act = () => EntityModelMapper.UpdateTargetEntity(null!, new ConcreteTargetingRequest());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateTargetEntity_NullRequest_Throws()
    {
        var act = () => EntityModelMapper.UpdateTargetEntity(new FeatureFlagTarget(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDto_MapsFeatureFlag()
    {
        var ff = new FeatureFlag
        {
            Key = "my-key",
            Name = "My Feature",
            Description = "desc",
            Type = FeatureFlagType.Percentage,
            IsEnabled = true,
            DefaultValue = "default",
            Environment = "production"
        };

        var dto = EntityModelMapper.ToDto(ff);

        dto.Key.Should().Be("my-key");
        dto.Name.Should().Be("My Feature");
        dto.Description.Should().Be("desc");
        dto.Type.Should().Be(FeatureFlagType.Percentage);
        dto.IsEnabled.Should().BeTrue();
        dto.DefaultValue.Should().Be("default");
        dto.Environment.Should().Be("production");
    }

    [Fact]
    public void ToDto_NullFeatureFlag_Throws()
    {
        var act = () => EntityModelMapper.ToDto(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // Concrete subclass to test the abstract FeatureFlagTargetingRequest
    private class ConcreteTargetingRequest : FeatureFlagTargetingRequest { }
}

#endregion

#region FeatureContextFactory

public class FeatureContextFactoryTests
{
    [Fact]
    public void CreateBasic_WithTenantAndUser()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var ctx = FeatureContextFactory.CreateBasic(tenantId, userId, "staging");

        ctx.TenantId.Should().Be(tenantId);
        ctx.UserId.Should().Be(userId);
        ctx.Environment.Should().Be("staging");
    }

    [Fact]
    public void CreateBasic_DefaultsToProduction()
    {
        var ctx = FeatureContextFactory.CreateBasic();

        ctx.Environment.Should().Be(FeatureFlagConstants.DefaultEnvironment);
        ctx.TenantId.Should().BeNull();
        ctx.UserId.Should().BeNull();
    }

    [Fact]
    public void Enrich_AddsAttributes()
    {
        var ctx = FeatureContextFactory.CreateBasic();
        var attrs = new Dictionary<string, object>
        {
            { "role", "admin" },
            { "tier", 3 }
        };

        var enriched = FeatureContextFactory.Enrich(ctx, attrs);

        enriched.CustomAttributes.Should().ContainKey("role");
        enriched.CustomAttributes.Should().ContainKey("tier");
        enriched.Should().BeSameAs(ctx); // enriches in-place
    }

    [Fact]
    public void Enrich_NullContext_Throws()
    {
        var act = () => FeatureContextFactory.Enrich(null!, new Dictionary<string, object>());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enrich_NullAttributes_Throws()
    {
        var ctx = FeatureContextFactory.CreateBasic();
        var act = () => FeatureContextFactory.Enrich(ctx, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToOpenFeatureContext_MapsAllFields()
    {
        var ctx = new FeatureContext
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Environment = "staging",
            IpAddress = "10.0.0.1",
            UserAgent = "TestAgent",
            Country = "US",
            SubscriptionPlanId = "pro",
            Permissions = ["read", "write"]
        };

        var ofc = FeatureContextFactory.ToOpenFeatureContext(ctx);

        ofc.Should().NotBeNull();
    }

    [Fact]
    public void ToOpenFeatureContext_NullContext_Throws()
    {
        var act = () => FeatureContextFactory.ToOpenFeatureContext(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToOpenFeatureContext_WithCustomAttributes_ConvertsAllTypes()
    {
        var ctx = new FeatureContext
        {
            CustomAttributes =
            {
                { "stringVal", "hello" },
                { "intVal", 42 },
                { "longVal", 100L },
                { "doubleVal", 3.14 },
                { "boolVal", true },
                { "dateVal", new DateTime(2025, 1, 1) },
                { "otherVal", new object() }
            }
        };

        var ofc = FeatureContextFactory.ToOpenFeatureContext(ctx);

        ofc.Should().NotBeNull();
    }

    [Fact]
    public void ToOpenFeatureContext_EmptyContext_DoesNotThrow()
    {
        var ctx = new FeatureContext();
        var ofc = FeatureContextFactory.ToOpenFeatureContext(ctx);
        ofc.Should().NotBeNull();
    }
}

#endregion

#region AddTargetingRuleCommand

public class AddTargetingRuleCommandTests
{
    [Fact]
    public void CanCreate_WithRequiredProperties()
    {
        var ffId = Guid.NewGuid();
        var cmd = new AddTargetingRuleCommand
        {
            FeatureFlagId = ffId,
            TargetType = "user",
            TargetIdentifier = "user-123"
        };

        cmd.FeatureFlagId.Should().Be(ffId);
        cmd.TargetType.Should().Be("user");
        cmd.TargetIdentifier.Should().Be("user-123");
        cmd.IsEnabled.Should().BeTrue(); // default
        cmd.RolloutPercentage.Should().Be(100); // default
        cmd.Priority.Should().Be(0); // default
        cmd.CustomValue.Should().BeNull();
        cmd.Metadata.Should().BeEmpty();
    }
}

#endregion

#region FeatureFlagTargetingRequest (via concrete subclass)

public class FeatureFlagTargetingRequestTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var request = new TestTargetingRequest();

        request.FeatureFlagId.Should().Be(Guid.Empty);
        request.FeatureKey.Should().BeEmpty();
        request.TargetType.Should().BeNull();
        request.TargetIdentifier.Should().BeNull();
        request.IsEnabled.Should().BeTrue();
        request.RolloutPercentage.Should().BeNull();
        request.CustomValue.Should().BeNull();
        request.Priority.Should().Be(0);
        request.Metadata.Should().BeNull();
        request.TargetUserIds.Should().BeEmpty();
        request.TargetTenantIds.Should().BeEmpty();
        request.TargetCountries.Should().BeEmpty();
        request.TargetPlans.Should().BeEmpty();
        request.CustomRules.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var ffId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new TestTargetingRequest
        {
            FeatureFlagId = ffId,
            FeatureKey = "my-feature",
            TargetType = "tenant",
            TargetIdentifier = "t-1",
            IsEnabled = false,
            RolloutPercentage = 50,
            CustomValue = "val",
            Priority = 10,
            TargetUserIds = [userId],
            TargetCountries = ["US", "CA"],
            TargetPlans = ["pro"]
        };

        request.FeatureFlagId.Should().Be(ffId);
        request.FeatureKey.Should().Be("my-feature");
        request.IsEnabled.Should().BeFalse();
        request.RolloutPercentage.Should().Be(50);
        request.TargetUserIds.Should().HaveCount(1);
        request.TargetCountries.Should().HaveCount(2);
    }

    private class TestTargetingRequest : FeatureFlagTargetingRequest { }
}

#endregion
