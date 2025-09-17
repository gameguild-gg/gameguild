namespace GameGuild.CQRS;

/// <summary> Represents a paged collection of items with metadata about pagination </summary>
/// <typeparam name="T"> The type of items in the collection </typeparam>
public record PagedResult<T> {
  /// <summary> Creates a new paged result </summary>
  /// <param name="items"> Items for this page </param>
  /// <param name="totalCount"> Total number of items </param>
  /// <param name="pageNumber"> Current page number </param>
  /// <param name="pageSize"> Items per page </param>
  public PagedResult(IEnumerable<T> items, long totalCount, int pageNumber, int pageSize) {
    Items = items;
    TotalCount = totalCount;
    PageNumber = pageNumber;
    PageSize = pageSize;
  }

  /// <summary> The items for the current page </summary>
  public IEnumerable<T> Items { get; init; } = [];

  /// <summary> Total number of items across all pages </summary>
  public long TotalCount { get; init; }

  /// <summary> Current page number (1-based) </summary>
  public int PageNumber { get; init; }

  /// <summary> Number of items per page </summary>
  public int PageSize { get; init; }

  /// <summary> Total number of pages </summary>
  public int TotalPages { get => PageSize == 0 ? 0 : (int) Math.Ceiling((double) TotalCount / PageSize); }

  /// <summary> Whether there is a previous page </summary>
  public bool HasPreviousPage { get => PageNumber > 1; }

  /// <summary> Whether there is a next page </summary>
  public bool HasNextPage { get => PageNumber < TotalPages; }

  /// <summary> Creates an empty paged result </summary>
  public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 10) { return new PagedResult<T>([], 0, pageNumber, pageSize); }
}
