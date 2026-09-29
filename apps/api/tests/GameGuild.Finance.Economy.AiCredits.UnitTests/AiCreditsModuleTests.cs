using FluentAssertions;
using GameGuild.AI;
using GameGuild.Finance.Economy.AiCredits;
using GameGuild.Finance.Economy.Integrations.AI;
using GameGuild.Finance.Economy.Queries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GameGuild.Finance.Economy.AiCredits.UnitTests;

public sealed class AiCreditsModuleTests
{
    [Fact]
    public void Registration_BindsPricingAndRegistersScopedProductServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Economy:AiCredits:DefaultInputSoftUnitsPerMillion"] = "1250",
            ["Economy:AiCredits:DefaultOutputSoftUnitsPerMillion"] = "4500",
        }).Build();

        services.AddAiCreditsModule(configuration).Should().BeSameAs(services);

        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(IAiCreditWalletService)
            && descriptor.ImplementationType == typeof(AiCreditWalletService) && descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(IEconomyWalletSoftUsageContributor)
            && descriptor.ImplementationType == typeof(AiCreditWalletSoftUsageContributor) && descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(IAiExecutionBillingRecorder)
            && descriptor.ImplementationType == typeof(AiCreditExecutionBillingRecorder) && descriptor.Lifetime == ServiceLifetime.Scoped);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AiCreditPricingOptions>>().Value;
        options.DefaultInputSoftUnitsPerMillion.Should().Be(1250);
        options.DefaultOutputSoftUnitsPerMillion.Should().Be(4500);
    }

    [Fact]
    public void Registration_WithoutPricingOverridesPreservesDefaults()
    {
        var services = new ServiceCollection();
        services.AddAiCreditsModule(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<AiCreditPricingOptions>>().Value;

        options.DefaultInputSoftUnitsPerMillion.Should().Be(2000);
        options.DefaultOutputSoftUnitsPerMillion.Should().Be(8000);
    }

    [Fact]
    public void Registration_RequiresAServiceCollection()
    {
        var act = () => AiCreditsModule.AddAiCreditsModule(null!, new ConfigurationBuilder().Build());

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("services");
    }

    [Fact]
    public void Registration_RequiresConfiguration()
    {
        var act = () => new ServiceCollection().AddAiCreditsModule(null!);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("configuration");
    }
}
