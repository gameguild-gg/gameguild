using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GameGuild.Features.UnitTests.Utilities;

public class EntityModelMapperTests
{
    [Fact]
    public void ToConfig_ShouldMapAllProperties()
    {
        var entity = CreateFeatureFlag();

        var config = EntityModelMapper.ToConfig(entity);

        config.Key.Should().Be("test-key");
        config.Name.Should().Be("Test Flag");
        config.Description.Should().Be("A test flag");
        config.Type.Should().Be(FeatureFlagType.Toggle);
        config.DefaultValue.Should().Be("false");
        config.EnabledValue.Should().Be("true");
        config.RolloutPercentage.Should().Be(75);
        config.Environment.Should().Be("production");
        config.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void ToConfig_ShouldThrow_WhenEntityIsNull()
    {
        var act = () => EntityModelMapper.ToConfig(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToConfigs_ShouldMapMultipleEntities()
    {
        var entities = new[] { CreateFeatureFlag(), CreateFeatureFlag("key-2", "Flag 2") };

        var configs = EntityModelMapper.ToConfigs(entities).ToList();

        configs.Should().HaveCount(2);
        configs[0].Key.Should().Be("test-key");
        configs[1].Key.Should().Be("key-2");
    }

    [Fact]
    public void ToConfigs_ShouldReturnEmpty_WhenCollectionIsEmpty()
    {
        var configs = EntityModelMapper.ToConfigs(Array.Empty<FeatureFlag>()).ToList();
        configs.Should().BeEmpty();
    }

    [Fact]
    public void ToConfigs_ShouldThrow_WhenNull()
    {
        var act = () => EntityModelMapper.ToConfigs(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToTargetingRule_ShouldMapProperties()
    {
        var target = new FeatureFlagTarget
        {
            TargetType = "tenant",
            TargetIdentifier = "t-123",
            IsEnabled = true,
            RolloutPercentage = 50,
            CustomValue = "custom",
            Priority = 3,
            Metadata = null
        };

        var rule = EntityModelMapper.ToTargetingRule(target);

        rule.TargetType.Should().Be("tenant");
        rule.TargetIdentifier.Should().Be("t-123");
        rule.IsEnabled.Should().BeTrue();
        rule.RolloutPercentage.Should().Be(50);
        rule.CustomValue.Should().Be("custom");
        rule.Priority.Should().Be(3);
        rule.Conditions.Should().BeEmpty();
    }

    [Fact]
    public void ToTargetingRule_ShouldDeserializeMetadata()
    {
        var metadata = new Dictionary<string, object> { { "key", "value" } };
        var target = new FeatureFlagTarget
        {
            TargetType = "user",
            TargetIdentifier = "u-1",
            IsEnabled = true,
            Metadata = JsonSerializer.Serialize(metadata)
        };

        var rule = EntityModelMapper.ToTargetingRule(target);

        rule.Conditions.Should().ContainKey("key");
    }

    [Fact]
    public void ToTargetingRule_ShouldThrow_WhenTargetIsNull()
    {
        var act = () => EntityModelMapper.ToTargetingRule(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToTargetEntity_ShouldMapRequestToEntity()
    {
        var request = CreateTargetingRequest();

        var entity = EntityModelMapper.ToTargetEntity(request);

        entity.FeatureFlagId.Should().Be(request.FeatureFlagId);
        entity.TargetType.Should().Be("user");
        entity.TargetIdentifier.Should().Be("u-123");
        entity.IsEnabled.Should().BeTrue();
        entity.RolloutPercentage.Should().Be(80);
        entity.CustomValue.Should().Be("custom-val");
        entity.Priority.Should().Be(2);
    }

    [Fact]
    public void ToTargetEntity_ShouldSetDefaultRollout_WhenNull()
    {
        var request = CreateTargetingRequest();
        request.RolloutPercentage = null;

        var entity = EntityModelMapper.ToTargetEntity(request);

        entity.RolloutPercentage.Should().Be(100);
    }

    [Fact]
    public void ToTargetEntity_ShouldSerializeMetadata_WhenNotEmpty()
    {
        var request = CreateTargetingRequest();
        request.Metadata = new Dictionary<string, object> { { "env", "staging" } };

        var entity = EntityModelMapper.ToTargetEntity(request);

        entity.Metadata.Should().NotBeNullOrEmpty();
        entity.Metadata.Should().Contain("env");
    }

    [Fact]
    public void ToTargetEntity_ShouldSetNullMetadata_WhenEmpty()
    {
        var request = CreateTargetingRequest();
        request.Metadata = new Dictionary<string, object>();

        var entity = EntityModelMapper.ToTargetEntity(request);

        entity.Metadata.Should().BeNull();
    }

    [Fact]
    public void ToTargetEntity_ShouldSetNullMetadata_WhenNull()
    {
        var request = CreateTargetingRequest();
        request.Metadata = null;

        var entity = EntityModelMapper.ToTargetEntity(request);

        entity.Metadata.Should().BeNull();
    }

    [Fact]
    public void ToTargetEntity_ShouldThrow_WhenRequestIsNull()
    {
        var act = () => EntityModelMapper.ToTargetEntity(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateTargetEntity_ShouldUpdateProperties()
    {
        var target = new FeatureFlagTarget
        {
            TargetType = "old",
            TargetIdentifier = "old-id",
            IsEnabled = false,
            RolloutPercentage = 10,
            Priority = 1,
            CustomValue = "old-val"
        };

        var request = CreateTargetingRequest();

        EntityModelMapper.UpdateTargetEntity(target, request);

        target.TargetType.Should().Be("user");
        target.TargetIdentifier.Should().Be("u-123");
        target.IsEnabled.Should().BeTrue();
        target.RolloutPercentage.Should().Be(80);
        target.Priority.Should().Be(2);
        target.CustomValue.Should().Be("custom-val");
    }

    [Fact]
    public void UpdateTargetEntity_ShouldNotUpdateCustomValue_WhenNull()
    {
        var target = new FeatureFlagTarget { CustomValue = "original" };
        var request = CreateTargetingRequest();
        request.CustomValue = null;

        EntityModelMapper.UpdateTargetEntity(target, request);

        target.CustomValue.Should().Be("original");
    }

    [Fact]
    public void UpdateTargetEntity_ShouldThrow_WhenTargetIsNull()
    {
        var act = () => EntityModelMapper.UpdateTargetEntity(null!, CreateTargetingRequest());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateTargetEntity_ShouldThrow_WhenRequestIsNull()
    {
        var act = () => EntityModelMapper.UpdateTargetEntity(new FeatureFlagTarget(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDto_ShouldMapEntityToDto()
    {
        var entity = CreateFeatureFlag();

        var dto = EntityModelMapper.ToDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.Key.Should().Be("test-key");
        dto.Name.Should().Be("Test Flag");
        dto.Description.Should().Be("A test flag");
        dto.Type.Should().Be(FeatureFlagType.Toggle);
        dto.IsEnabled.Should().BeTrue();
        dto.DefaultValue.Should().Be("false");
        dto.Environment.Should().Be("production");
    }

    [Fact]
    public void ToDto_ShouldThrow_WhenNull()
    {
        var act = () => EntityModelMapper.ToDto(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToModel_ShouldConvertType()
    {
        var result = EntityModelMapper.ToModel(FeatureFlagType.Toggle);
        result.Should().Be(FeatureFlagType.Toggle);
    }

    [Fact]
    public void ToEntity_ShouldConvertType()
    {
        var result = EntityModelMapper.ToEntity(FeatureFlagType.String);
        result.Should().Be(FeatureFlagType.String);
    }

    private static FeatureFlag CreateFeatureFlag(string key = "test-key", string name = "Test Flag")
    {
        return new FeatureFlag
        {
            Key = key,
            Name = name,
            Description = "A test flag",
            Type = FeatureFlagType.Toggle,
            IsEnabled = true,
            DefaultValue = "false",
            EnabledValue = "true",
            RolloutPercentage = 75,
            Environment = "production"
        };
    }

    private static TestTargetingRequest CreateTargetingRequest()
    {
        return new TestTargetingRequest
        {
            FeatureFlagId = Guid.NewGuid(),
            TargetType = "user",
            TargetIdentifier = "u-123",
            IsEnabled = true,
            RolloutPercentage = 80,
            CustomValue = "custom-val",
            Priority = 2,
            Metadata = null
        };
    }

    private class TestTargetingRequest : FeatureFlagTargetingRequest;
}
