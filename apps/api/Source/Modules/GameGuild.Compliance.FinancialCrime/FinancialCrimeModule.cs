using GameGuild.Economy.Risk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameGuild.Compliance.FinancialCrime;

public sealed class FinancialCrimeModule : ModuleBase
{
    public override string Name => "Compliance.FinancialCrime";
    public override bool EnabledByDefault => false;
    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.AddFinancialCrimeComposition();
}

public static class FinancialCrimeCompositionExtensions
{
    public static IServiceCollection AddFinancialCrimeComposition(this IServiceCollection services)
    {
        services.TryAddSingleton<IFinancialCrimeRiskInputSource, DisabledFinancialCrimeRiskInputSource>();
        return services;
    }
}

internal sealed class DisabledFinancialCrimeRiskInputSource : IFinancialCrimeRiskInputSource
{
    public ValueTask<FinancialCrimeRiskInput> ReadAsync(
        string opaqueSubjectReference,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(opaqueSubjectReference);
        return ValueTask.FromResult(new FinancialCrimeRiskInput(
            1,
            observedAt,
            observedAt.AddMinutes(1),
            ExternalRiskOutcome.Unavailable,
            "financial-crime-composition-disabled",
            true));
    }
}
