using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameGuild.Social.Profiles;

public static class DependencyInjection
{
    public static IServiceCollection AddSocialProfilesModule(this IServiceCollection services)
    {
        services.AddScoped<ISocialProfileRepository, SocialProfileRepository>();
        services.AddScoped<IProfileSkillRepository, ProfileSkillRepository>();
        services.AddScoped<IProfilePortfolioRepository, ProfilePortfolioRepository>();
        services.AddScoped<ISocialProfileService, SocialProfileService>();

        // Issue #1315 SP1: the follow-graph port defaults to the fail-closed null
        // implementation; the Social.Follows module registers the real adapter, and
        // TryAdd keeps whichever registration already exists, so composition order
        // cannot reintroduce the fail-open default.
        services.TryAddScoped<IProfileFollowerPort, NullProfileFollowerPort>();
        return services;
    }
}
