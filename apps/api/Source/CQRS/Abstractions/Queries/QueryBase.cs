namespace GameGuild.CQRS;

/// <summary> Base record for queries </summary>
/// <typeparam name="TResponse"> The response type </typeparam>
public abstract record QueryBase<TResponse> : IQuery<TResponse> {
  /// <summary> Unique identifier for the query </summary>
  public Guid QueryId { get; init; } = Guid.NewGuid();

  /// <summary> When the query was created </summary>
  public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
