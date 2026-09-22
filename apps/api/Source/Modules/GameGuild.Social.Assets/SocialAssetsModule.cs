using GameGuild.Social.Assets.SocialMedia;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Social.Assets;

/// <summary>
/// Module registration for the social assets module.
/// </summary>
public static class SocialAssetsModule
{
    /// <summary>
    /// Adds social assets module services to the DI container.
    /// </summary>
    public static IServiceCollection AddSocialAssetsModule(this IServiceCollection services)
    {
        services.AddScoped<ISocialMediaAssetService, SocialMediaAssetService>();

        return services;
    }
}
