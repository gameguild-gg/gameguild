using GameGuild.Compliance.FinancialCrime;
using GameGuild.Finance.Economy.Risk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.API.Core.Integration;

public static class FinancialCrimeEconomyIngestionExtensions
{
    /// <summary>
    /// Wires the option-C economy hook (issue #1291 FC1): <see cref="EconomyFinancialCrimeSignalAdapter"/>
    /// decorates Economy's trusted protected operation authorizer so every value-moving flow
    /// (top-up mint, conversion, transfer, payout execution, marketplace settlement) feeds the
    /// AML engine, and every successful operation consumes the subject's unconsumed Approved
    /// case decision. Follows the #1294 tenant-guard port-inversion: the adapter lives in the
    /// FinancialCrime module and implements an Economy-owned interface (FinancialCrime already
    /// references Economy, so no cycle), while the host composition performs the binding.
    /// </summary>
    /// <remarks>
    /// Registration order is load-bearing (security-architecture deployment invariant #2,
    /// same last-wins rule as the evidence readers): this MUST be called AFTER
    /// <c>AddEconomyCoreComposition</c>, so the interface registration resolved by the flows is
    /// the adapter wrapping the Economy default, not the bare default.
    /// </remarks>
    public static IServiceCollection AddFinancialCrimeEconomyIngestion(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        // Self-registration of the Economy default so the decorator can resolve its inner
        // authorizer; the interface registration below (last-wins) is the live one.
        services.TryAddScoped<EconomyTrustedProtectedOperationAuthorizer>();
        services.AddScoped<IEconomyTrustedProtectedOperationAuthorizer>(provider =>
            new EconomyFinancialCrimeSignalAdapter(
                provider.GetRequiredService<EconomyTrustedProtectedOperationAuthorizer>(),
                provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<IOptions<FinancialCrimeEconomyHookOptions>>(),
                provider.GetRequiredService<TimeProvider>(),
                provider.GetService<ILogger<EconomyFinancialCrimeSignalAdapter>>()));
        return services;
    }
}
