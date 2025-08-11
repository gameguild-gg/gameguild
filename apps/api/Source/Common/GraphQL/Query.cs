namespace GameGuild.Common;

/// <summary>
/// Root GraphQL Query type - entry point for all GraphQL queries
/// Extended by module-specific query types using [ExtendObjectType]
/// </summary>
public class Query {
  /// <summary>
  /// Health check query to ensure GraphQL is working
  /// </summary>
  [GraphQLDescription("Health check query to ensure GraphQL is working")]
  public string Health() => "GraphQL API is healthy";
}
