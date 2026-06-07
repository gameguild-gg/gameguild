using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Social.Profiles;

public static class DependencyInjection
{
    public static IServiceCollection AddSocialProfilesModule(this IServiceCollection services)
    {
        services.AddScoped<ISocialProfileRepository, SocialProfileRepository>();
        services.AddScoped<IProfileSkillRepository, ProfileSkillRepository>();
        services.AddScoped<IProfilePortfolioRepository, ProfilePortfolioRepository>();
        services.AddScoped<ISocialProfileService, SocialProfileService>();
        return services;
    }
}
