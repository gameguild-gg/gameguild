using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.Bounties.UnitTests;

public sealed class BountiesModuleTests
{
    [Fact]
    public void ModuleRemainsDisabledByDefaultAndRegistersOnlyThePersistentStore()
    {
        var module = new BountiesModule();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        module.Name.Should().Be("Economy.Bounties");
        module.EnabledByDefault.Should().BeFalse();
        module.ConfigureServices(services, configuration).Should().BeSameAs(services);
        services.AddBountiesComposition(configuration).Should().BeSameAs(services);
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IBountyEscrowStore) &&
            descriptor.ImplementationType == typeof(PostgreSqlBountyEscrowStore) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}
