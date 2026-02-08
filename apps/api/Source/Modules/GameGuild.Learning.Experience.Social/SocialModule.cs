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
        // Register individual services
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IDiscussionService, DiscussionService>();
        services.AddScoped<IReplyService, ReplyService>();
        services.AddScoped<ILikeService, LikeService>();
        services.AddScoped<IFeedService, FeedService>();

        return services;
    }
}
