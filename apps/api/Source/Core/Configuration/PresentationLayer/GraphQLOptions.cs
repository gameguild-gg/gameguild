namespace GameGuild;

public class GraphQlOptions {
  public bool EnableIntrospection { get; set; } = false;

  public bool EnableGraphiQl { get; set; } = false;

  public int MaxDepth { get; set; } = 10;

  public int MaxComplexity { get; set; } = 1000;

  public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

  public string Path { get; set; } = "/graphql";

  public void Validate() {
    if (MaxDepth <= 0) throw new InvalidOperationException("GraphQL max depth must be greater than 0.");

    if (MaxComplexity <= 0) throw new InvalidOperationException("GraphQL max complexity must be greater than 0.");

    if (Timeout <= TimeSpan.Zero) throw new InvalidOperationException("GraphQL timeout must be greater than zero.");

    if (string.IsNullOrWhiteSpace(Path)) throw new InvalidOperationException("GraphQL path cannot be null or empty.");

    if (!Path.StartsWith("/")) throw new InvalidOperationException("GraphQL path must start with '/'.");
  }
}
