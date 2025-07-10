namespace GameGuild.Common.Models;

/// <summary>
/// Represents a paginated result set
/// </summary>
public class PagedResult<T>
{
    public PagedResult(IEnumerable<T> items, int totalCount, int skip, int take)
    {
        Items = items.ToList();
        TotalCount = totalCount;
        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// The items in the current page
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Number of items skipped
    /// </summary>
    public int Skip { get; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int Take { get; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber => (Skip / Take) + 1;

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Take);

    /// <summary>
    /// Whether there are more pages after this one
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Whether there are pages before this one
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
}
