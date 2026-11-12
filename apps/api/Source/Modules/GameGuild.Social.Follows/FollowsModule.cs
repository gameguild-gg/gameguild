using GameGuild.Social.Follows.Services;
using GameGuild.Social.Profiles;
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
        services.AddScoped<IUserModerationService, UserModerationService>();
        services.AddScoped<IFollowOperationService, FollowOperationService>();
        services.AddScoped<IFollowerService, FollowerService>();

        // Issue #1315 SP1: the real follower entitlement for Private/Connections profile
        // reads. Registered with Add (not TryAdd) so it replaces the Profiles module's
        // fail-closed NullProfileFollowerPort default regardless of composition order.
        services.AddScoped<IProfileFollowerPort, SocialFollowsProfileFollowerPort>();
        return services;
    }
}
