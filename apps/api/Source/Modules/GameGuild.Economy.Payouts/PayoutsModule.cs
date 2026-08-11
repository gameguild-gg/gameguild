using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.Payouts;

public sealed class PayoutsModule : ModuleBase
{
    public override string Name => "Economy.Payouts";
    public override bool EnabledByDefault => true;

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPayoutOperationStore, PostgreSqlPayoutOperationStore>();

        // Read-only payout status is safe without a payout provider. Write workflows remain
        // opt-in because they require provider evidence verification and execution gates.
        if (configuration.GetValue<bool>("Modules:Economy.Payouts:WriteWorkflowEnabled"))
        {
            services.AddScoped<IDurablePayoutReservationWorkflow, PostgreSqlDurablePayoutReservationWorkflow>();
            services.AddScoped<IDurablePayoutSettlementWorkflow, PostgreSqlDurablePayoutSettlementWorkflow>();
        }

        return services;
    }
}

public static class PayoutsCompositionExtensions
{
    public static IServiceCollection AddPayoutsComposition(
        this IServiceCollection services,
        IConfiguration configuration) => new PayoutsModule().ConfigureServices(services, configuration);
}
