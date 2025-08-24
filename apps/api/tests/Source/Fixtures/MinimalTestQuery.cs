namespace GameGuild.Tests.Fixtures;

/// <summary>
/// Minimal GraphQL query type for testing basic GraphQL functionality
/// </summary>
public class MinimalTestQuery {
  /// <summary>
  /// Simple test query that doesn't use CQRS - for GraphQL infrastructure testing
  /// </summary>
  public Task<string> GetTestMessage() {
    return Task.FromResult("Hello from GraphQL infrastructure test!");
  }

  /// <summary>
  /// Another simple test query
  /// </summary>
  public Task<string> GetStatus() {
    return Task.FromResult("GraphQL server is working");
  }
}
