using GameGuild;
using GameGuild.Finance.Economy.Integrations.AI;

namespace GameGuild.Finance.Economy.AiCredits;

/// <summary>
/// Entity framework model configuration for the AI-credit economy module. It
/// owns the wallet reservation and rate card entities that bill AI execution
/// soft units against the shared economy wallet balance.
/// </summary>
public sealed class AiCreditsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new AiCreditReservationConfiguration());
        modelBuilder.ApplyConfiguration(new AiCreditRateCardConfiguration());
    }
}
