using FluentAssertions;
using GameGuild.Economy.Funding;
using GameGuild.Economy.Ledger;
using GameGuild.Economy.Risk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GameGuild.Economy.UnitTests;

public sealed class EconomyCoreModuleTests
{
    [Fact]
    public void CoreModuleDefaultsToDisabledAndRegistersDurableGateways()
    {
        var services = new ServiceCollection();
        var module = new EconomyCoreModule();

        module.EnabledByDefault.Should().BeFalse();
        module.Name.Should().Be("Economy.Core");
        module.ConfigureServices(services, new ConfigurationBuilder().Build());

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IRegisteredPostingGateway) &&
            descriptor.ImplementationType == typeof(PostgreSqlRegisteredPostingGateway));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IHardCoinFundingGateway) &&
            descriptor.ImplementationType == typeof(PostgreSqlHardCoinFundingGateway));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IRiskDecisionAuthorizer) &&
            descriptor.ImplementationType == typeof(PostgreSqlRiskDecisionAuthorizer));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IHardToSoftConversionWorkflow) &&
            descriptor.ImplementationType == typeof(PostgreSqlHardToSoftConversionWorkflow));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IHardToSoftConversionRiskEvidenceVerifier) &&
            descriptor.ImplementationType == typeof(HardToSoftConversionRiskEvidenceVerifier));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IHardToSoftConversionRiskDecisionIssuer) &&
            descriptor.ImplementationType == typeof(PostgreSqlHardToSoftConversionRiskDecisionIssuer));
        services.Should().NotContain(descriptor =>
            descriptor.ServiceType == typeof(InMemoryLedgerKernelStore) ||
            descriptor.ImplementationType == typeof(RiskDecisionAuthorizer));
    }

    [Fact]
    public void CompositionExtensionRegistersTheCoreModule()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var result = services.AddEconomyCoreComposition(configuration);

        result.Should().BeSameAs(services);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IHardToSoftConversionGateway) &&
            descriptor.ImplementationType == typeof(PostgreSqlHardToSoftConversionGateway));
    }
    [Fact]
    public void ValueMovementCapabilities_AreExplicitAndFailClosed()
    {
        EconomyValueMovementCapabilities.Parse(null).Should().BeEmpty();
        EconomyValueMovementCapabilities.Parse(["convertHardToSoft"])
            .Should().ContainSingle().Which.Should().Be(EconomyValueMovementCapability.ConvertHardToSoft);

        var disabled = new EconomyValueMovementDecisionGate(Options.Create(new EconomyRiskCompositionOptions
        {
            EnabledCapabilities = ["ConvertHardToSoft"]
        }));
        disabled.IsEnabled.Should().BeFalse();
        disabled.IsCapabilityEnabled(EconomyValueMovementCapability.ConvertHardToSoft).Should().BeFalse();
        Action disabledAction = disabled.EnsureEnabled;
        disabledAction.Should().Throw<EconomyValueMovementDisabledException>();

        var enabled = new EconomyValueMovementDecisionGate(Options.Create(new EconomyRiskCompositionOptions
        {
            ValueMovingDecisionsEnabled = true,
            EnabledCapabilities = ["ConvertHardToSoft"]
        }));
        enabled.EnsureEnabled();
        enabled.IsCapabilityEnabled(EconomyValueMovementCapability.ConvertHardToSoft).Should().BeTrue();
        enabled.IsCapabilityEnabled(EconomyValueMovementCapability.Transfer).Should().BeFalse();
        Action missingCapability = () => enabled.EnsureEnabled(EconomyValueMovementCapability.Transfer);
        missingCapability.Should().Throw<EconomyValueMovementDisabledException>();

        Action missingOptions = () => EconomyValueMovementCapabilities.Validate(new EconomyRiskCompositionOptions
        {
            ValueMovingDecisionsEnabled = true
        });
        Action unknownCapability = () => EconomyValueMovementCapabilities.Parse(["not-a-capability"]);
        Action nullOptions = () => EconomyValueMovementCapabilities.Validate(null!);

        missingOptions.Should().Throw<EconomyCapabilityConfigurationException>();
        unknownCapability.Should().Throw<EconomyCapabilityConfigurationException>();
        nullOptions.Should().Throw<ArgumentNullException>();
    }
}
