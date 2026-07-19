using GameGuild.Compliance.FinancialCrime;
using GameGuild.Economy.Risk;
using GameGuild.TrustSafety;

namespace GameGuild.API.Setup;

public static class EconomyCapabilityCompositionExtensions
{
    public static IServiceCollection AddEconomyCapabilityComposition(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddEconomyRiskComposition(configuration);
        services.AddFinancialCrimeComposition();
        services.AddTrustSafetyComposition();
        return services;
    }
}
