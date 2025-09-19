using GameGuild.Core.Modules;
using GameGuild.Modules.Posts.GraphQL;
using GameGuild.Modules.Posts.Services;
using HotChocolate.Execution.Configuration;

namespace GameGuild.Source.Modules.Posts;

/// <summary>
/// Posts module implementing the standardized IModule interface.
/// Provides comprehensive posts management services following Clean Architecture.
/// </summary>
[StandardizedModule("Comprehensive posts management services following Clean Architecture")]
[ModuleVersion("1.0.0")]
public class PostsModule : ModuleBase {
  public override string ModuleName => "Posts";
  public override string ModuleVersion => "1.0.0";

  public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration) {
    base.ConfigureServices(services, configuration);

    // Register Posts module services
    services.AddScoped<IPostAnnouncementService, PostAnnouncementService>();

    // Register GraphQL DataLoaders
    services.AddScoped<IUserDataLoader, UserDataLoader>();
    services.AddScoped<IPostContentReferenceDataLoader, PostContentReferenceDataLoader>();
    services.AddScoped<IPostCommentDataLoader, PostCommentDataLoader>();
    services.AddScoped<IPostLikeDataLoader, PostLikeDataLoader>();

    // Domain event handlers are automatically registered by GameGuild.CQRS
    // via the assembly scanning in AddOptimizedHandlers

    return services;
  }

  public override WebApplication MapEndpoints(WebApplication app) {
    base.MapEndpoints(app);

    // Posts module doesn't have specific middleware currently
    // This can be extended when needed for posts-specific routes or middleware

    return app;
  }

  /// <summary>
  /// Registers Posts GraphQL schema components
  /// </summary>
  /// <param name="builder">The request executor builder</param>
  /// <returns>The request executor builder for chaining</returns>
  public static IRequestExecutorBuilder AddPostsGraphQL(IRequestExecutorBuilder builder) {
    return builder.AddType<PostType>().AddTypeExtension<PostQueries>().AddTypeExtension<PostMutations>();
  }
}

/// <summary>
/// Extension methods for the Posts module providing the standardized pattern.
/// </summary>
public static class PostsModuleExtensions {
  /// <summary>
  /// Registers the Posts module using the IModule pattern.
  /// </summary>
  /// <param name="services">The service collection</param>
  /// <param name="configuration">The application configuration</param>
  /// <returns>The service collection for chaining</returns>
  public static IServiceCollection AddPostsModule(this IServiceCollection services, IConfiguration configuration) {
    return services.AddModule<PostsModule>(configuration);
  }

  /// <summary>
  /// Maps Posts module endpoints using the IModule pattern.
  /// </summary>
  /// <param name="app">The web application</param>
  /// <returns>The web application for chaining</returns>
  public static WebApplication UsePostsModule(this WebApplication app) {
    return app.UseModule<PostsModule>();
  }
}
