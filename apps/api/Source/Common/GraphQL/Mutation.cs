namespace GameGuild.Common;

/// <summary>
/// Root GraphQL Mutation type - entry point for all GraphQL mutations
/// Extended by module-specific mutation types using [ExtendObjectType]
/// </summary>
public class Mutation {
  /// <summary>
  /// Health check mutation to ensure GraphQL mutations are working
  /// </summary>
  public string HealthMutation() => "GraphQL mutations are healthy";
}
