using FluentAssertions;

namespace GameGuild.Features.UnitTests;

public class SpecificationsTests
{
    [Fact]
    public void EnabledFeatureFlagsSpecification_ShouldCreate()
    {
        var spec = new EnabledFeatureFlagsSpecification();
        spec.Should().NotBeNull();
        spec.Criteria.Should().NotBeNull();
    }

    [Fact]
    public void FeatureFlagByKeySpecification_ShouldCreate()
    {
        var spec = new FeatureFlagByKeySpecification("my-flag");
        spec.Should().NotBeNull();
        spec.Criteria.Should().NotBeNull();
    }

    [Fact]
    public void FeatureFlagsByTypeSpecification_ShouldCreate()
    {
        var spec = new FeatureFlagsByTypeSpecification(FeatureFlagType.Toggle);
        spec.Should().NotBeNull();
        spec.Criteria.Should().NotBeNull();
    }

    [Fact]
    public void GlobalFeatureFlagsSpecification_ShouldCreate()
    {
        var spec = new GlobalFeatureFlagsSpecification();
        spec.Should().NotBeNull();
        spec.Criteria.Should().NotBeNull();
    }

    [Fact]
    public void TenantSpecificFeatureFlagsSpecification_WithGuid_ShouldCreate()
    {
        var tenantId = Guid.NewGuid();
        var spec = new TenantSpecificFeatureFlagsSpecification(tenantId);
        spec.Should().NotBeNull();
        spec.Criteria.Should().NotBeNull();
    }

    [Fact]
    public void TenantSpecificFeatureFlagsSpecification_WithString_ShouldCreate()
    {
        var tenantId = Guid.NewGuid();
        var spec = new TenantSpecificFeatureFlagsSpecification(tenantId.ToString());
        spec.Should().NotBeNull();
    }

    [Fact]
    public void FeatureFlagSpecifications_ByKey_ShouldReturnSpec()
    {
        var spec = FeatureFlagSpecifications.ByKey("test");
        spec.Should().NotBeNull();
    }

    [Fact]
    public void FeatureFlagSpecifications_EnabledFlags_ShouldReturnSpec()
    {
        var spec = FeatureFlagSpecifications.EnabledFlags();
        spec.Should().NotBeNull();
    }

    [Fact]
    public void FeatureFlagSpecifications_GlobalFlags_ShouldReturnSpec()
    {
        var spec = FeatureFlagSpecifications.GlobalFlags();
        spec.Should().NotBeNull();
    }

    [Fact]
    public void FeatureFlagSpecifications_TenantSpecificFlags_ShouldReturnSpec()
    {
        var spec = FeatureFlagSpecifications.TenantSpecificFlags(Guid.NewGuid());
        spec.Should().NotBeNull();
    }

    [Fact]
    public void FeatureFlagSpecifications_ByType_ShouldReturnSpec()
    {
        var spec = FeatureFlagSpecifications.ByType(FeatureFlagType.Percentage);
        spec.Should().NotBeNull();
    }
}

public class CommandsAndQueriesTests
{
    [Fact]
    public void CreateFeatureFlagCommand_ShouldSetProperties()
    {
        var cmd = new CreateFeatureFlagCommand("key", "name", "desc", true, Guid.NewGuid());
        cmd.Key.Should().Be("key");
        cmd.Name.Should().Be("name");
        cmd.Description.Should().Be("desc");
        cmd.IsEnabled.Should().BeTrue();
        cmd.TenantId.Should().NotBeNull();
    }

    [Fact]
    public void CreateFeatureFlagCommand_Defaults()
    {
        var cmd = new CreateFeatureFlagCommand("key", "name", null);
        cmd.IsEnabled.Should().BeFalse();
        cmd.TenantId.Should().BeNull();
    }

    [Fact]
    public void CreateFeatureFlagRequest_ShouldSetDefaults()
    {
        var req = new CreateFeatureFlagRequest();
        req.Key.Should().BeEmpty();
        req.Name.Should().BeEmpty();
        req.Description.Should().BeNull();
        req.IsEnabled.Should().BeTrue();
        req.RolloutPercentage.Should().Be(100);
        req.Environment.Should().Be("production");
        req.Tags.Should().BeNull();
    }

    [Fact]
    public void CreateFeatureCommand_ShouldSetProperties()
    {
        var cmd = new CreateFeatureCommand("key", "name", "desc");
        cmd.Key.Should().Be("key");
        cmd.Name.Should().Be("name");
        cmd.Description.Should().Be("desc");
    }

    [Fact]
    public void CreateFeatureRequest_ShouldSetProperties()
    {
        var req = new CreateFeatureRequest("key", "name", "desc", true, Guid.NewGuid());
        req.Key.Should().Be("key");
        req.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void ToggleFeatureFlagCommand_ShouldSetProperties()
    {
        var id = Guid.NewGuid();
        var cmd = new ToggleFeatureFlagCommand(id, true);
        cmd.Id.Should().Be(id);
        cmd.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void UpdateFeatureFlagCommand_ShouldSetProperties()
    {
        var id = Guid.NewGuid();
        var cmd = new UpdateFeatureFlagCommand(id, "name", "desc", true, 50, "on", "off");
        cmd.Id.Should().Be(id);
        cmd.Name.Should().Be("name");
        cmd.RolloutPercentage.Should().Be(50);
    }

    [Fact]
    public void AddTargetingRuleCommand_ShouldSetDefaults()
    {
        var cmd = new AddTargetingRuleCommand
        {
            FeatureFlagId = Guid.NewGuid(),
            TargetType = "tenant",
            TargetIdentifier = "t-1"
        };
        cmd.IsEnabled.Should().BeTrue();
        cmd.RolloutPercentage.Should().Be(100);
        cmd.CustomValue.Should().BeNull();
        cmd.Priority.Should().Be(0);
        cmd.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void EnableFeatureFlagCommand_ShouldSetId()
    {
        var id = Guid.NewGuid();
        var cmd = new EnableFeatureFlagCommand(id);
        cmd.Id.Should().Be(id);
    }

    [Fact]
    public void DisableFeatureFlagCommand_ShouldSetId()
    {
        var id = Guid.NewGuid();
        var cmd = new DisableFeatureFlagCommand(id);
        cmd.Id.Should().Be(id);
    }

    [Fact]
    public void GetFeatureFlagByIdQuery_ShouldSetId()
    {
        var id = Guid.NewGuid();
        var query = new GetFeatureFlagByIdQuery { Id = id };
        query.Id.Should().Be(id);
    }

    [Fact]
    public void SearchFeatureFlagsQuery_ShouldSetDefaults()
    {
        var query = new SearchFeatureFlagsQuery();
        query.SearchTerm.Should().BeNull();
        query.Environment.Should().BeNull();
        query.IsEnabled.Should().BeNull();
        query.IsGlobal.Should().BeNull();
        query.Type.Should().BeNull();
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(10);
    }

    [Fact]
    public void ValidateFeatureFlagKeyQuery_ShouldSetProperties()
    {
        var query = new ValidateFeatureFlagKeyQuery { Key = "my-flag" };
        query.Key.Should().Be("my-flag");
        query.ExcludeId.Should().BeNull();
    }

    [Fact]
    public void GetFeatureFlagStatisticsQuery_ShouldSetDefaults()
    {
        var query = new GetFeatureFlagStatisticsQuery();
        query.Environment.Should().BeNull();
        query.StartDate.Should().BeNull();
        query.EndDate.Should().BeNull();
    }

    [Fact]
    public void GetTargetingRulesQuery_ShouldSetProperties()
    {
        var id = Guid.NewGuid();
        var query = new GetTargetingRulesQuery { FeatureFlagId = id };
        query.FeatureFlagId.Should().Be(id);
    }
}

public class CustomTargetingHandlerTests
{
    private readonly CustomTargetingHandler _handler = new();

    [Fact]
    public void Priority_ShouldBe5()
    {
        _handler.Priority.Should().Be(5);
    }

    [Fact]
    public async Task EvaluateAsync_NoCustomTarget_ShouldReturnNull()
    {
        var flag = new FeatureFlag { Key = "test-flag", Name = "Test Flag" };
        var context = new FeatureContext();

        var result = await _handler.EvaluateAsync(flag, context);
        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_CustomTargetMatches_ShouldReturnEnabled()
    {
        var flag = new FeatureFlag { Key = "test-flag", Name = "Test Flag" };
        var target = new FeatureFlagTarget
        {
            TargetType = "custom",
            TargetIdentifier = "plan=pro",
            IsEnabled = true,
            RolloutPercentage = 100
        };
        flag.Targets.Add(target);

        var context = new FeatureContext
        {
            CustomAttributes = new Dictionary<string, object> { { "plan", "pro" } }
        };

        var result = await _handler.EvaluateAsync(flag, context);
        result.Should().NotBeNull();
        result!.IsEnabled.Should().BeTrue();
        result.IsTargeted.Should().BeTrue();
        result.TargetType.Should().Be("custom");
        result.Reason.Should().Be("Custom attributes match");
    }

    [Fact]
    public async Task EvaluateAsync_CustomTargetNotMatching_ShouldReturnNull()
    {
        var flag = new FeatureFlag { Key = "test-flag", Name = "Test Flag" };
        var target = new FeatureFlagTarget
        {
            TargetType = "custom",
            TargetIdentifier = "plan=enterprise",
            IsEnabled = true,
            RolloutPercentage = 100
        };
        flag.Targets.Add(target);

        var context = new FeatureContext
        {
            CustomAttributes = new Dictionary<string, object> { { "plan", "free" } }
        };

        var result = await _handler.EvaluateAsync(flag, context);
        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_EmptyCustomAttributes_ShouldReturnNull()
    {
        var flag = new FeatureFlag { Key = "test-flag", Name = "Test Flag" };
        var target = new FeatureFlagTarget
        {
            TargetType = "custom",
            TargetIdentifier = "plan=pro",
            IsEnabled = true,
            RolloutPercentage = 100
        };
        flag.Targets.Add(target);

        var context = new FeatureContext(); // No custom attributes

        var result = await _handler.EvaluateAsync(flag, context);
        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_DisabledTarget_ShouldReturnDisabled()
    {
        var flag = new FeatureFlag { Key = "test-flag", Name = "Test Flag", DefaultValue = "off" };
        var target = new FeatureFlagTarget
        {
            TargetType = "custom",
            TargetIdentifier = "plan=pro",
            IsEnabled = false,
            RolloutPercentage = 100
        };
        flag.Targets.Add(target);

        var context = new FeatureContext
        {
            CustomAttributes = new Dictionary<string, object> { { "plan", "pro" } }
        };

        var result = await _handler.EvaluateAsync(flag, context);
        result.Should().NotBeNull();
        result!.IsEnabled.Should().BeFalse();
        result.Value.Should().Be("off");
    }
}
