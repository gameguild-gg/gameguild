using GameGuild.Economy.Risk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameGuild.TrustSafety;

public sealed class TrustSafetyModule : ModuleBase
{
    public override string Name => "TrustSafety";
    public override bool EnabledByDefault => false;
    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.AddTrustSafetyComposition();
}

public static class TrustSafetyCompositionExtensions
{
    public static IServiceCollection AddTrustSafetyComposition(this IServiceCollection services)
    {
        services.TryAddSingleton<ITrustSafetyRiskInputSource, DisabledTrustSafetyRiskInputSource>();
        return services;
    }
}

internal sealed class DisabledTrustSafetyRiskInputSource : ITrustSafetyRiskInputSource
{
    public ValueTask<TrustSafetyRiskInput> ReadAsync(
        string opaqueSubjectReference,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(opaqueSubjectReference);
        return ValueTask.FromResult(new TrustSafetyRiskInput(
            1,
            observedAt,
            observedAt.AddMinutes(1),
            ExternalRiskOutcome.Unavailable,
            "trust-safety-composition-disabled",
            true));
    }
}
