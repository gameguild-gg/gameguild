using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.AdRewards;

public sealed class AdRewardsModule : ModuleBase
{
    public override string Name => "Economy.AdRewards";
    public override bool EnabledByDefault => false;

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services;
}

public static class AdRewardsCompositionExtensions
{
    public static IServiceCollection AddAdRewardsComposition(
        this IServiceCollection services,
        IConfiguration configuration) => new AdRewardsModule().ConfigureServices(services, configuration);
}
