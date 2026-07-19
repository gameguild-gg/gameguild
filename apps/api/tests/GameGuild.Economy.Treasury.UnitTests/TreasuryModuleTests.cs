using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.Treasury.UnitTests;

public sealed class TreasuryModuleTests
{
    [Fact]
    public void ModuleAndCompositionHookRemainDisabledAndCannotMutateCore()
    {
        var module = new TreasuryModule();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        module.Name.Should().Be("Economy.Treasury");
        module.EnabledByDefault.Should().BeFalse();
        module.ConfigureServices(services, configuration).Should().BeSameAs(services);
        services.AddTreasuryComposition(configuration).Should().BeSameAs(services);
        services.Should().BeEmpty();
    }
}
