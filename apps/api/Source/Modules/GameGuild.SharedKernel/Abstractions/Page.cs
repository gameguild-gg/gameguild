namespace GameGuild.Abstractions;

/// <summary>
///     Represents a page of items with pagination information
/// </summary>
/// <typeparam name="T">The type of items in the page</typeparam>
public class Page<T> : IPage<T>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Page{T}" /> class
    /// </summary>
    /// <param name="items">The items in the current page</param>
    /// <param name="pageNumber">The current page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="totalCount">The total number of items across all pages</param>
    public Page(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        PageNumber = pageNumber > 0 ? pageNumber : throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0");
        PageSize = pageSize > 0 ? pageSize : throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0");
        TotalCount = totalCount >= 0 ? totalCount : throw new ArgumentOutOfRangeException(nameof(totalCount), "Total count must be non-negative");
    }

    /// <inheritdoc />
    public IEnumerable<T> Items { get; }

    /// <inheritdoc />
    public int PageNumber { get; }

    /// <inheritdoc />
    public int PageSize { get; }

    /// <inheritdoc />
    public int TotalCount { get; }

    /// <inheritdoc />
    public int TotalPages { get => (int) Math.Ceiling(TotalCount / (double) PageSize); }

    /// <inheritdoc />
    public bool HasPreviousPage { get => PageNumber > 1; }

    /// <inheritdoc />
    public bool HasNextPage { get => PageNumber < TotalPages; }

    /// <summary>
    ///     Creates an empty page
    /// </summary>
    /// <returns>An empty page</returns>
    public static Page<T> Empty() { return new Page<T>([], 1, 10, 0); }

    /// <summary>
    ///     Creates a page from a list of items and total count
    /// </summary>
    /// <param name="items">The items in the current page</param>
    /// <param name="pageNumber">The current page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="totalCount">The total number of items across all pages</param>
    /// <returns>A new page instance</returns>
    public static Page<T> Create(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount) { return new Page<T>(items, pageNumber, pageSize, totalCount); }
}
