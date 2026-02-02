using GameGuild.Learning.Experience.Social.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Learning.Experience.Social;

/// <summary>
/// Module registration for Social Learning services
/// </summary>
public static class SocialModule
{
    /// <summary>
    /// Adds Social Learning module services to the DI container
    /// </summary>
    public static IServiceCollection AddSocialModule(this IServiceCollection services)
    {
        services.AddScoped<ISocialService, SocialService>();

        return services;
    }
}
