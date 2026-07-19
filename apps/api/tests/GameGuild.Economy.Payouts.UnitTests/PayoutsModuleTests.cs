using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.Payouts.UnitTests;

public sealed class PayoutsModuleTests
{
    [Fact]
    public void ModuleAndCompositionHookRemainDisabledAndRouteFree()
    {
        var module = new PayoutsModule();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        module.Name.Should().Be("Economy.Payouts");
        module.EnabledByDefault.Should().BeFalse();
        module.ConfigureServices(services, configuration).Should().BeSameAs(services);
        services.AddPayoutsComposition(configuration).Should().BeSameAs(services);
        services.Should().BeEmpty();
    }
}
