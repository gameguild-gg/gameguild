using FluentAssertions;
using GameGuild.Economy.Risk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.UnitTests.Risk;

public sealed class EconomyRiskCompositionTests
{
    [Fact]
    public void ValueMovingDecisionsAreDisabledByDefault()
    {
        var services = new ServiceCollection();
        services.AddEconomyRiskComposition(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();
        var gate = provider.GetRequiredService<IEconomyValueMovementDecisionGate>();

        gate.IsEnabled.Should().BeFalse();
        FluentActions.Invoking(gate.EnsureEnabled)
            .Should().Throw<EconomyValueMovementDisabledException>();
    }

    [Fact]
    public void ValueMovingDecisionsRequireExplicitConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            [$"{EconomyRiskCompositionOptions.SectionName}:ValueMovingDecisionsEnabled"] = "true"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddEconomyRiskComposition(configuration);

        using var provider = services.BuildServiceProvider();
        var gate = provider.GetRequiredService<IEconomyValueMovementDecisionGate>();

        gate.IsEnabled.Should().BeTrue();
        FluentActions.Invoking(gate.EnsureEnabled).Should().NotThrow();
    }

    [Fact]
    public void CompositionRejectsMissingDependencies()
    {
        FluentActions.Invoking(() => EconomyRiskCompositionExtensions.AddEconomyRiskComposition(
                null!, new ConfigurationBuilder().Build()))
            .Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new ServiceCollection().AddEconomyRiskComposition(null!))
            .Should().Throw<ArgumentNullException>();
    }
}
