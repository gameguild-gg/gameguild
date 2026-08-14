using Microsoft.EntityFrameworkCore;

namespace GameGuild.Resources;

/// <summary>
/// Registers the Resources bounded-context entities in the shared application model.
/// </summary>
public sealed class ResourcesModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ResourcesModelConfiguration).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Resources", StringComparison.Ordinal) == true);
    }
}
