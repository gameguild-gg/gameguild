using GameGuild.Economy.Ledger;
using GameGuild.Economy.Funding;
using GameGuild.Economy.Integrations;
using GameGuild.Economy.Integrations.AI;
using GameGuild.Economy.Risk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy;

public sealed class EconomyCoreModule : ModuleBase
{
    public override string Name => "Economy.Core";
    public override bool EnabledByDefault => false;

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddEconomyRiskComposition(configuration);
        services.AddOptions<SelfServiceHardToSoftRiskDecisionOptions>()
            .Bind(configuration.GetSection(SelfServiceHardToSoftRiskDecisionOptions.SectionName));
        services.AddScoped<IRiskDecisionAuthorizer, PostgreSqlRiskDecisionAuthorizer>();
        services.AddScoped<IHardToSoftConversionRiskEvidenceVerifier, HardToSoftConversionRiskEvidenceVerifier>();
        services.AddScoped<IHardToSoftConversionRiskDecisionIssuer, PostgreSqlHardToSoftConversionRiskDecisionIssuer>();
        services.AddScoped<CoreProtectedPostingGate>();
        services.AddScoped<IRegisteredPostingGateway, PostgreSqlRegisteredPostingGateway>();
        services.AddScoped<IHardCoinFundingGateway, PostgreSqlHardCoinFundingGateway>();
        services.AddScoped<IHardToSoftConversionGateway, PostgreSqlHardToSoftConversionGateway>();
        services.AddScoped<IHardToSoftConversionWorkflow, PostgreSqlHardToSoftConversionWorkflow>();
        services.AddScoped<IFifoFragmentReservationGateway, PostgreSqlFifoFragmentReservationGateway>();
        services.AddScoped<IProviderReversalGateway, PostgreSqlProviderReversalGateway>();
        services.AddScoped<IFifoTransferGateway, PostgreSqlFifoTransferGateway>();
        services.AddSingleton<IStripeEconomyFundingAdapter, StripeEconomyFundingAdapter>();
        services.AddScoped<IAiProviderCostFactStore, EfAiProviderCostFactStore>();
        return services;
    }
}

public static class EconomyCoreCompositionExtensions
{
    public static IServiceCollection AddEconomyCoreComposition(
        this IServiceCollection services,
        IConfiguration configuration) => new EconomyCoreModule().ConfigureServices(services, configuration);
}
