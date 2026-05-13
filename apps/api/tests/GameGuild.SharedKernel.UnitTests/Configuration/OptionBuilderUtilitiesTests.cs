using FluentAssertions;
using Microsoft.Extensions.Configuration;
using GameGuild.Configuration;
using GameGuild.Configuration.PresentationLayer;

namespace GameGuild.Tests.SharedKernel.Unit.Configuration;

public class OptionBuilderUtilitiesTests
{
    [Fact]
    public void CreateAndBind_WithExistingSection_BindsValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestSection:IsEnabled"] = "false"
            })
            .Build();

        var result = OptionBuilderUtilities.CreateAndBind(configuration, "TestSection", () => new TestConfigOptions());

        result.Should().NotBeNull();
        result.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void CreateAndBind_WithNonExistentSection_ReturnsDefaults()
    {
        var configuration = new ConfigurationBuilder().Build();

        var result = OptionBuilderUtilities.CreateAndBind(configuration, "Missing", () => new TestConfigOptions());

        result.Should().NotBeNull();
        result.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void CreateBindAndValidate_CallsValidator()
    {
        var configuration = new ConfigurationBuilder().Build();
        var validated = false;

        var result = OptionBuilderUtilities.CreateBindAndValidate(
            configuration,
            "TestSection",
            () => new TestConfigOptions(),
            _ => validated = true);

        result.Should().NotBeNull();
        validated.Should().BeTrue();
    }

    [Fact]
    public void CreateBindAndValidate_WithNullValidator_DoesNotThrow()
    {
        var configuration = new ConfigurationBuilder().Build();

        var result = OptionBuilderUtilities.CreateBindAndValidate<TestConfigOptions>(
            configuration,
            "TestSection",
            () => new TestConfigOptions(),
            null);

        result.Should().NotBeNull();
    }
}

public class GenericOptionsBuilderTests
{
    [Fact]
    public void Create_ReturnsNewInstance()
    {
        var result = OptionsBuilder<TestConfigOptions>.Create();

        result.Should().NotBeNull();
    }

    [Fact]
    public void Create_FromConfiguration_BindsSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MySection:IsEnabled"] = "false"
            })
            .Build();

        var result = OptionsBuilder<TestConfigOptions>.Create(configuration, "MySection");

        result.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Build_CreatesAndValidates()
    {
        var configuration = new ConfigurationBuilder().Build();

        var result = OptionsBuilder<TestConfigOptions>.Build(configuration, "TestSection");

        result.Should().NotBeNull();
    }
}

public class TopLevelPresentationLayerOptionsBuilderTests
{
    [Fact]
    public void CreateDefault_ReturnsEnabledDefaults()
    {
        var options = PresentationLayerOptionsBuilder.CreateDefault();

        options.EnableOpenApi.Should().BeTrue();
        options.EnableCors.Should().BeTrue();
    }

    [Fact]
    public void CreateWithValidation_FromConfiguration_DoesNotThrow()
    {
        var configuration = new ConfigurationBuilder().Build();
        var act = () => PresentationLayerOptionsBuilder.CreateWithValidation(configuration);

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_FromConfiguration_BindsValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PresentationLayer:EnableCors"] = "false"
            })
            .Build();

        var options = PresentationLayerOptionsBuilder.Create(configuration);

        options.EnableCors.Should().BeFalse();
    }
}

public sealed class TestConfigOptions : BaseOptions
{
}
