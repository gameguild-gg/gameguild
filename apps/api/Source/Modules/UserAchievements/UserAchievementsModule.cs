using HotChocolate.Execution.Configuration;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Extension methods for registering UserAchievements module services
/// </summary>
public static class UserAchievementsModule {
  /// <summary>
  /// Registers all UserAchievements module services and event handlers
  /// </summary>
  public static IServiceCollection AddUserAchievementsModule(this IServiceCollection services) {
    // Register module services
    services.AddScoped<IAchievementService, AchievementService>();
    services.AddScoped<IAchievementNotificationService, AchievementNotificationService>();

    // Register GraphQL DataLoaders
    services.AddScoped<IAchievementDataLoader, AchievementDataLoader>();
    services.AddScoped<IUserAchievementDataLoader, UserAchievementDataLoader>();
    services.AddScoped<IAchievementLevelDataLoader, AchievementLevelDataLoader>();
    services.AddScoped<IAchievementPrerequisiteDataLoader, AchievementPrerequisiteDataLoader>();
    services.AddScoped<IAchievementStatisticsDataLoader, AchievementStatisticsDataLoader>();
    services.AddScoped<IAchievementEarnCountDataLoader, AchievementEarnCountDataLoader>();
    // Note: IUserDataLoader is provided by Posts module

    // Domain event handlers are automatically registered by MediatR
    // via the assembly scanning in AddOptimizedHandlers

    return services;
  }

  /// <summary>
  /// Registers UserAchievements GraphQL schema components
  /// </summary>
  public static IRequestExecutorBuilder AddUserAchievementsGraphQL(this IRequestExecutorBuilder builder) {
    return builder
           .AddType<AchievementType>()
           .AddType<UserAchievementType>()
           .AddType<AchievementLevelType>()
           .AddType<AchievementProgressType>()
           .AddType<AchievementStatisticsType>()
           .AddTypeExtension<AchievementQueries>()
           .AddTypeExtension<AchievementMutations>();
  }
}
