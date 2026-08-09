using FluentAssertions;
using GameGuild.Economy.Ledger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.UnitTests;

public sealed class EconomyCoreModuleTests
{
    [Fact]
    public void CoreModuleDefaultsToDisabledAndRegistersOnlyTheDurableGateway()
    {
        var services = new ServiceCollection();
        var module = new EconomyCoreModule();

        module.EnabledByDefault.Should().BeFalse();
        module.Name.Should().Be("Economy.Core");
        module.ConfigureServices(services, new ConfigurationBuilder().Build());

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IRegisteredPostingGateway) &&
            descriptor.ImplementationType == typeof(PostgreSqlRegisteredPostingGateway));
        services.Should().NotContain(descriptor =>
            descriptor.ServiceType == typeof(InMemoryLedgerKernelStore));
    }
}
