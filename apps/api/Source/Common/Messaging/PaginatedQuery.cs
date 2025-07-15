namespace GameGuild.Common;

/// <summary>
/// Base class for paginated queries
/// </summary>
public abstract class PaginatedQuery<TResponse> : IQuery<PagedResult<TResponse>> {
  /// <summary>
  /// Number of items to skip (for pagination)
  /// </summary>
  public int Skip { get; set; } = 0;

  /// <summary>
  /// Number of items to take (page size)
  /// </summary>
  public int Take { get; set; } = 50;

  /// <summary>
  /// Search term for filtering
  /// </summary>
  public string? SearchTerm { get; set; }

  /// <summary>
  /// Include soft-deleted entities
  /// </summary>
  public bool IncludeDeleted { get; set; } = false;
}
