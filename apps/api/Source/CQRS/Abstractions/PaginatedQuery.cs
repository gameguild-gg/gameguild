namespace GameGuild.CQRS;

/// <summary>
/// Base class for paginated queries
/// </summary>
/// <typeparam name="TResult">The type of result returned by the query</typeparam>
public abstract class PaginatedQuery<TResult> : IQuery<GameGuild.PagedResult<TResult>> {
  /// <summary>
  /// Page number (1-based)
  /// </summary>
  public int PageNumber { get; set; } = 1;

  /// <summary>
  /// Number of items per page
  /// </summary>
  public int PageSize { get; set; } = 20;

  /// <summary>
  /// Optional search term
  /// </summary>
  public string? SearchTerm { get; set; }

  /// <summary>
  /// Optional sorting field
  /// </summary>
  public string? SortBy { get; set; }

  /// <summary>
  /// Sort direction
  /// </summary>
  public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

  /// <summary>
  /// Validates the pagination parameters
  /// </summary>
  public virtual void Validate() {
    if (PageNumber < 1)
      throw new ArgumentOutOfRangeException(nameof(PageNumber), "Page number must be greater than 0");

    if (PageSize < 1 || PageSize > 100)
      throw new ArgumentOutOfRangeException(nameof(PageSize), "Page size must be between 1 and 100");
  }
}

/// <summary>
/// Sort direction for paginated queries
/// </summary>
public enum SortDirection {
  Ascending,
  Descending
}
