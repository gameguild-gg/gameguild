using FluentAssertions;
using GameGuild.API.Setup;
using GameGuild.Compliance.FinancialCrime;
using GameGuild.Economy.Risk;
using GameGuild.TrustSafety;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.API.UnitTests.Core;

public sealed class EconomyCapabilityCompositionTests
{
    [Fact]
    public async Task ApiComposesFailClosedFinancialRiskInputsWithoutEnablingCapabilities()
    {
        var services = new ServiceCollection();
        services.AddEconomyCapabilityComposition(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();
        var gate = provider.GetRequiredService<IEconomyValueMovementDecisionGate>();
        var financialCrime = provider.GetRequiredService<IFinancialCrimeRiskInputSource>();
        var trustSafety = provider.GetRequiredService<ITrustSafetyRiskInputSource>();
        var now = new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

        gate.IsEnabled.Should().BeFalse();
        (await financialCrime.ReadAsync("actor-token", now)).Outcome
            .Should().Be(ExternalRiskOutcome.Unavailable);
        (await trustSafety.ReadAsync("actor-token", now)).Outcome
            .Should().Be(ExternalRiskOutcome.Unavailable);
    }
}
