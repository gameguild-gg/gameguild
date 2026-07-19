using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GameGuild.Economy.Risk;

public sealed class EconomyRiskCompositionOptions
{
    public const string SectionName = "Modules:Economy.Risk";

    public bool ValueMovingDecisionsEnabled { get; set; }
}

public interface IEconomyValueMovementDecisionGate
{
    bool IsEnabled { get; }

    void EnsureEnabled();
}

internal sealed class EconomyValueMovementDecisionGate(
    IOptions<EconomyRiskCompositionOptions> options) : IEconomyValueMovementDecisionGate
{
    public bool IsEnabled => options.Value.ValueMovingDecisionsEnabled;

    public void EnsureEnabled()
    {
        if (!IsEnabled)
            throw new EconomyValueMovementDisabledException(
                "Economy value-moving decisions are disabled until an explicit capability rollout is configured.");
    }
}

public static class EconomyRiskCompositionExtensions
{
    public static IServiceCollection AddEconomyRiskComposition(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddOptions<EconomyRiskCompositionOptions>()
            .Bind(configuration.GetSection(EconomyRiskCompositionOptions.SectionName));
        services.TryAddSingleton<IEconomyValueMovementDecisionGate, EconomyValueMovementDecisionGate>();
        return services;
    }
}

public sealed class EconomyValueMovementDisabledException(string message) : InvalidOperationException(message);
