namespace GameGuild;

/// <summary>
///     Non-generic interface exposing pagination metadata for use in filters and middleware
///     that need to access pagination properties without knowing the item type.
///     Implemented by <see cref="PagedResult{T}"/>.
/// </summary>
public interface IPaginationMetadata
{
    /// <summary>The total number of items across all pages</summary>
    int TotalCount { get; }

    /// <summary>Number of items skipped (offset)</summary>
    int Skip { get; }

    /// <summary>Number of items per page</summary>
    int Take { get; }

    /// <summary>Current page number (1-based)</summary>
    int PageNumber { get; }

    /// <summary>Total number of pages</summary>
    int TotalPages { get; }

    /// <summary>Whether a next page exists</summary>
    bool HasNextPage { get; }

    /// <summary>Whether a previous page exists</summary>
    bool HasPreviousPage { get; }
}
