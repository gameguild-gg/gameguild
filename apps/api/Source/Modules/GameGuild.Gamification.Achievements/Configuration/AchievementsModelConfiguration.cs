using Microsoft.EntityFrameworkCore;

namespace GameGuild.Gamification.Achievements;

/// <summary>
///     EF Core model configuration bridge for the Gamification.Achievements module
///     (issue #1158). Discovered by ApplicationDbContext via IModelConfiguration assembly
///     scanning — without this bridge the module's per-entity configurations are never
///     applied and the achievement tables never materialize, even when the assembly is
///     referenced.
/// </summary>
public sealed class AchievementsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Achievement).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Gamification.Achievements", StringComparison.Ordinal) == true);
    }
}
