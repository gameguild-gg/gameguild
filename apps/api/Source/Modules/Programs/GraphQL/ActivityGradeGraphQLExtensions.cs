namespace GameGuild.Modules.Programs;

/// <summary>
/// Extension methods for registering ActivityGrade GraphQL components
/// </summary>
public static class ActivityGradeGraphQLExtensions {
  /// <summary>
  /// Registers ActivityGrade GraphQL types, queries, and mutations
  /// </summary>
  public static IServiceCollection AddActivityGradeGraphQL(this IServiceCollection services) {
    services
      .AddGraphQLServer()
      .AddType<ActivityGradeType>()
      .AddTypeExtension<ActivityGradeQueries>()
      .AddTypeExtension<ActivityGradeMutations>();

    return services;
  }
}
