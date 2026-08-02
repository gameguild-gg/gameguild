using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.Marketplace;

public sealed class MarketplaceModule : ModuleBase
{
    public override string Name => "Economy.Marketplace";
    public override bool EnabledByDefault => false;
    public override IServiceCollection ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration) => services;
}

public static class MarketplaceCompositionExtensions
{
    public static IServiceCollection AddMarketplaceComposition(
        this IServiceCollection services,
        IConfiguration configuration) =>
        new MarketplaceModule().ConfigureServices(services, configuration);
}
