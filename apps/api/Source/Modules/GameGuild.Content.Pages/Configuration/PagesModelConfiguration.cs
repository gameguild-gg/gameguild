using Microsoft.EntityFrameworkCore;

namespace GameGuild.Content.Pages;

/// <summary>
///     EF Core model configuration for the Content.Pages module.
///     Discovered by the main API database context via assembly scanning.
/// </summary>
public sealed class PagesModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Page).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Content.Pages", StringComparison.Ordinal) == true);
    }
}
