using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.Payouts.UnitTests;

public sealed class PayoutsModuleTests
{
    [Fact]
    public void ModuleEnablesReadOnlyPayoutStatusWithoutWriteWorkflows()
    {
        var module = new PayoutsModule();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        module.Name.Should().Be("Economy.Payouts");
        module.EnabledByDefault.Should().BeTrue();
        module.ConfigureServices(services, configuration).Should().BeSameAs(services);
        services.AddPayoutsComposition(configuration).Should().BeSameAs(services);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IPayoutOperationStore) &&
            descriptor.ImplementationType == typeof(PostgreSqlPayoutOperationStore) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().NotContain(descriptor =>
            descriptor.ServiceType == typeof(IDurablePayoutReservationWorkflow));
        services.Should().NotContain(descriptor =>
            descriptor.ServiceType == typeof(IDurablePayoutSettlementWorkflow));
        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(PayoutCoordinator));
        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(DbContext));
    }

    [Fact]
    public void WriteWorkflowsRequireExplicitConfiguration()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Modules:Economy.Payouts:WriteWorkflowEnabled"] = "true"
            })
            .Build();

        new PayoutsModule().ConfigureServices(services, configuration);

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IDurablePayoutReservationWorkflow) &&
            descriptor.ImplementationType == typeof(PostgreSqlDurablePayoutReservationWorkflow) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IDurablePayoutSettlementWorkflow) &&
            descriptor.ImplementationType == typeof(PostgreSqlDurablePayoutSettlementWorkflow) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}
