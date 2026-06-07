namespace GameGuild;

/// <summary>
///     Represents a page of items with pagination information
/// </summary>
/// <typeparam name="T">The type of items in the page</typeparam>
public interface IPage<out T>
{
    /// <summary>
    ///     The items in the current page
    /// </summary>
    IEnumerable<T> Items { get; }

    /// <summary>
    ///     The current page number (1-based)
    /// </summary>
    int PageNumber { get; }

    /// <summary>
    ///     The number of items per page
    /// </summary>
    int PageSize { get; }

    /// <summary>
    ///     The total number of items across all pages
    /// </summary>
    int TotalCount { get; }

    /// <summary>
    ///     The total number of pages
    /// </summary>
    int TotalPages { get; }

    /// <summary>
    ///     Indicates whether there is a previous page
    /// </summary>
    bool HasPreviousPage { get; }

    /// <summary>
    ///     Indicates whether there is a next page
    /// </summary>
    bool HasNextPage { get; }
}
