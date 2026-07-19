using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.AdRewards.UnitTests;

public sealed class AdRewardsModuleTests
{
    [Fact]
    public void ModuleAndCompositionHookRemainDisabledAndRouteFree()
    {
        var module = new AdRewardsModule();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        module.Name.Should().Be("Economy.AdRewards");
        module.EnabledByDefault.Should().BeFalse();
        module.ConfigureServices(services, configuration).Should().BeSameAs(services);
        services.AddAdRewardsComposition(configuration).Should().BeSameAs(services);
        services.Should().BeEmpty();
    }
}
