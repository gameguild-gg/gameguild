using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.Bounties;

public sealed class BountiesModule : ModuleBase
{
    public override string Name => "Economy.Bounties";
    public override bool EnabledByDefault => false;
    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services;
}

public static class BountiesCompositionExtensions
{
    public static IServiceCollection AddBountiesComposition(
        this IServiceCollection services,
        IConfiguration configuration) => new BountiesModule().ConfigureServices(services, configuration);
}
