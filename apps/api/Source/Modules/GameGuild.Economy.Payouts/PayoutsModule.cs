using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.Payouts;

public sealed class PayoutsModule : ModuleBase
{
    public override string Name => "Economy.Payouts";
    public override bool EnabledByDefault => false;

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPayoutOperationStore, PostgreSqlPayoutOperationStore>();
        return services;
    }
}

public static class PayoutsCompositionExtensions
{
    public static IServiceCollection AddPayoutsComposition(
        this IServiceCollection services,
        IConfiguration configuration) => new PayoutsModule().ConfigureServices(services, configuration);
}
