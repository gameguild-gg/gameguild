using GameGuild.Common;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Execution.Configuration;

namespace GameGuild.Modules.Posts;

/// <summary>
/// Extension methods for registering Posts module services
/// </summary>
public static class PostsModule
{
    /// <summary>
    /// Registers all Posts module services and event handlers
    /// </summary>
    public static IServiceCollection AddPostsModule(this IServiceCollection services)
    {
        // Register Posts module services
        services.AddScoped<IPostAnnouncementService, PostAnnouncementService>();

        // Register GraphQL DataLoaders
        services.AddScoped<IUserDataLoader, UserDataLoader>();
        services.AddScoped<IPostContentReferenceDataLoader, PostContentReferenceDataLoader>();
        services.AddScoped<IPostCommentDataLoader, PostCommentDataLoader>();
        services.AddScoped<IPostLikeDataLoader, PostLikeDataLoader>();

        // Domain event handlers are automatically registered by MediatR
        // via the assembly scanning in AddOptimizedHandlers

        return services;
    }

    /// <summary>
    /// Registers Posts GraphQL schema components
    /// </summary>
    public static IRequestExecutorBuilder AddPostsGraphQL(this IRequestExecutorBuilder builder)
    {
        return builder
            .AddType<PostType>()
            .AddTypeExtension<PostQueries>()
            .AddTypeExtension<PostMutations>();
    }
}
