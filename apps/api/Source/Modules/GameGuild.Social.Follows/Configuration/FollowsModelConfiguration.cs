using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Follows.Configuration;

/// <summary>
/// Registers Social.Follows entities in the composed application model.
/// </summary>
public sealed class FollowsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FollowsModelConfiguration).Assembly);
    }
}
