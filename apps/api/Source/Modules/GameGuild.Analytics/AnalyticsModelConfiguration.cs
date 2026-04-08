using Microsoft.EntityFrameworkCore;

namespace GameGuild.Analytics;

/// <summary>
///     EF Core model configuration for the Analytics module.
///     Auto-discovered by ApplicationDbContext via assembly scanning.
/// </summary>
public class AnalyticsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AnalyticsModelConfiguration).Assembly);
    }
}
