using Microsoft.EntityFrameworkCore;

namespace GameGuild.Assets.Configuration;

/// <summary>
///     EF Core model configuration for the Assets module.
/// </summary>
public sealed class AssetsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AssetsModelConfiguration).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Assets", StringComparison.Ordinal) == true);
    }
}
