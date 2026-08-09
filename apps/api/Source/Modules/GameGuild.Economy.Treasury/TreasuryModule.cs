using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.Treasury;

public sealed class TreasuryModule : ModuleBase
{
    public override string Name => "Economy.Treasury";
    public override bool EnabledByDefault => false;

    public override IServiceCollection ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<IAdminWithdrawalStore, PostgreSqlAdminWithdrawalStore>();
        services.AddScoped<IAdminWithdrawalAuditTrail, PostgreSqlAdminWithdrawalAuditTrail>();
        return services;
    }
}

public static class TreasuryCompositionExtensions
{
    public static IServiceCollection AddTreasuryComposition(
        this IServiceCollection services,
        IConfiguration configuration) => new TreasuryModule().ConfigureServices(services, configuration);
}
