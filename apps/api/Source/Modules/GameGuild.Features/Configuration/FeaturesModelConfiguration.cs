using Microsoft.EntityFrameworkCore;

namespace GameGuild.Features;

/// <summary>
///     EF Core model configuration for the Features module.
/// </summary>
public sealed class FeaturesModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FeatureFlag).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Features", StringComparison.Ordinal) == true);
    }
}
