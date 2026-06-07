using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Profiles;

public sealed class SocialProfilesModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SocialProfilesModelConfiguration).Assembly);
    }
}
