using GameGuild.AI;
using GameGuild.Finance.Economy.Integrations.AI;
using GameGuild.Finance.Economy.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Finance.Economy.AiCredits;

/// <summary>
/// Contributes AI-credit reservations and settlements to the shared economy
/// wallet soft balance. Reserved soft units are surfaced as held soft units and
/// both reserved and settled units reduce the available soft balance.
/// </summary>
public sealed class AiCreditWalletSoftUsageContributor(
    IApplicationDbContext db) : IEconomyWalletSoftUsageContributor
{
    public async Task<EconomyWalletSoftUsage> GetUsageAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var usage = await db.Set<AiCreditReservation>()
            .AsNoTracking()
            .Where(row => row.WalletId == walletId)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Reserved = group
                    .Where(row => row.Status == AiCreditReservationStatus.Reserved)
                    .Sum(row => row.ReservedSoftUnits),
                Settled = group
                    .Where(row => row.Status == AiCreditReservationStatus.Settled)
                    .Sum(row => row.SettledSoftUnits),
            })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return new EconomyWalletSoftUsage(usage?.Reserved ?? 0, usage?.Settled ?? 0);
    }
}

/// <summary>
/// Records terminal AI execution outcomes for the AI-credit economy domain.
/// Wallet charging stays caller-driven through the reserve and settle flow;
/// this recorder attributes every billed execution to the AI-credit domain for
/// operational visibility.
/// </summary>
public sealed class AiCreditExecutionBillingRecorder(
    ILogger<AiCreditExecutionBillingRecorder> logger) : IAiExecutionBillingRecorder
{
    public ValueTask RecordExecutionAsync(AiExecutionBillingRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        logger.LogInformation(
            "AI credit execution attributed for tenant {TenantId} actor {ActorId}: provider {Provider} model {Model} outcome {Outcome} input {InputTokens} output {OutputTokens} total {TotalTokens}",
            record.TenantId,
            record.ActorId,
            record.Provider,
            record.Model,
            record.Outcome,
            record.InputTokens,
            record.OutputTokens,
            record.TotalTokens);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Module registration for the AI-credit economy module.
/// </summary>
public static class AiCreditsModule
{
    /// <summary>
    /// Adds AI-credit economy module services to the DI container.
    /// </summary>
    public static IServiceCollection AddAiCreditsModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AiCreditPricingOptions>().Bind(configuration.GetSection(AiCreditPricingOptions.SectionName));
        services.AddScoped<IAiCreditWalletService, AiCreditWalletService>();
        services.AddScoped<IEconomyWalletSoftUsageContributor, AiCreditWalletSoftUsageContributor>();
        services.AddScoped<IAiExecutionBillingRecorder, AiCreditExecutionBillingRecorder>();

        return services;
    }
}
