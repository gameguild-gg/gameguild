using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Ratings;

/// <summary>
///     EF Core model configuration bridge for the Social.Ratings module (issue #1158).
///     Discovered by ApplicationDbContext via IModelConfiguration assembly scanning —
///     without this bridge the module's per-entity configurations are never applied and
///     the rating tables never materialize, even when the assembly is referenced.
/// </summary>
public sealed class RatingsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Rating).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Social.Ratings", StringComparison.Ordinal) == true);
    }
}
