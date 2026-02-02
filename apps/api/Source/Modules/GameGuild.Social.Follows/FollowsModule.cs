using GameGuild.Social.Follows.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Social.Follows;

/// <summary>
/// Module registration for GameGuild.Social.Follows
/// </summary>
public static class FollowsModule
{
    /// <summary>
    /// Adds the Follows module services to the service collection
    /// </summary>
    public static IServiceCollection AddFollowsModule(this IServiceCollection services)
    {
        services.AddScoped<IFollowerService, FollowerService>();
        return services;
    }
}
