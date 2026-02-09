
namespace GameGuild;

/// <summary>
///     Represents a paginated result set with full pagination metadata.
///     This is the single canonical pagination type — use it everywhere.
/// </summary>
/// <remarks>
///     Supports both offset-based (skip/take) and page-based (pageNumber/pageSize) construction.
///     All properties are always available regardless of which constructor is used.
/// </remarks>
public class PagedResult<T> : IPage<T>, IPaginationMetadata
{
    /// <summary>
    ///     Creates a paged result from offset-based (skip/take) parameters.
    /// </summary>
    public PagedResult(IEnumerable<T> items, int totalCount, int skip, int take)
    {
        Items = items as IReadOnlyList<T> ?? items.ToList();
        TotalCount = totalCount;
        Skip = skip;
        Take = take;
        PageSize = take;
        PageNumber = take > 0 ? skip / take + 1 : 1;
    }

    /// <summary>
    ///     Creates a paged result from page-based (pageNumber/pageSize) parameters.
    /// </summary>
    public static PagedResult<T> FromPage(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        var skip = (pageNumber - 1) * pageSize;
        return new PagedResult<T>(items, totalCount, skip, pageSize);
    }

    /// <summary>
    ///     Creates an empty paged result.
    /// </summary>
    public static PagedResult<T> Empty(int pageSize = 10)
        => new([], 0, 0, pageSize);

    // ── Items ────────────────────────────────────────────────────────────

    /// <summary>
    ///     The items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    // ── IPage<T>.Items — covariant enumerable ────────────────────────────
    IEnumerable<T> IPage<T>.Items => Items;

    // ── Counts ───────────────────────────────────────────────────────────

    /// <summary>
    ///     Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    // ── Offset-based properties ──────────────────────────────────────────

    /// <summary>
    ///     Number of items skipped (offset).
    /// </summary>
    public int Skip { get; }

    /// <summary>
    ///     Number of items requested per page (alias for <see cref="PageSize" />).
    /// </summary>
    public int Take { get; }

    // ── Page-based properties ────────────────────────────────────────────

    /// <summary>
    ///     Current page number (1-based).
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    ///     Number of items per page.
    /// </summary>
    public int PageSize { get; }

    // ── Computed navigation ──────────────────────────────────────────────

    /// <summary>
    ///     Total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    ///     Whether there are more items after this page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    ///     Whether there are pages before this one.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
}
