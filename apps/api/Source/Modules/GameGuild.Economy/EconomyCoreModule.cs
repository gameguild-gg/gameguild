using GameGuild.Economy.Ledger;
using GameGuild.Economy.Funding;
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
        services.AddScoped<IRegisteredPostingGateway, PostgreSqlRegisteredPostingGateway>();
        services.AddScoped<IHardCoinFundingGateway, PostgreSqlHardCoinFundingGateway>();
        services.AddScoped<IFifoFragmentReservationGateway, PostgreSqlFifoFragmentReservationGateway>();
        services.AddScoped<IFifoTransferGateway, PostgreSqlFifoTransferGateway>();
        return services;
    }
}

public static class EconomyCoreCompositionExtensions
{
    public static IServiceCollection AddEconomyCoreComposition(
        this IServiceCollection services,
        IConfiguration configuration) => new EconomyCoreModule().ConfigureServices(services, configuration);
}
