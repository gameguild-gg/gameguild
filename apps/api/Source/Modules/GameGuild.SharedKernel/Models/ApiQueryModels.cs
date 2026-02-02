using System.ComponentModel.DataAnnotations;

namespace GameGuild.Models;

/// <summary>
/// Standard query parameters for paginated API endpoints.
/// Provides consistent pagination interface across all APIs.
/// </summary>
public record PaginationParams
{
    /// <summary>
    /// Number of items to skip (offset-based pagination)
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Skip { get; init; } = 0;

    /// <summary>
    /// Number of items to return (page size)
    /// </summary>
    [Range(1, 100)]
    public int Take { get; init; } = 20;

    /// <summary>
    /// Cursor for cursor-based pagination (optional, takes precedence over skip)
    /// </summary>
    public string? Cursor { get; init; }
}

/// <summary>
/// Standard sorting parameters for API endpoints.
/// Supports multi-field sorting with explicit direction.
/// </summary>
public record SortingParams
{
    /// <summary>
    /// Field to sort by. Common values: CreatedAt, UpdatedAt, Name, Title, Rating
    /// </summary>
    public string? Sort { get; init; }

    /// <summary>
    /// Sort direction: "asc" or "desc"
    /// </summary>
    public string Order { get; init; } = "desc";

    /// <summary>
    /// Whether to sort in descending order
    /// </summary>
    public bool IsDescending => Order.Equals("desc", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Combined query parameters for listing endpoints.
/// Includes pagination, sorting, and optional search.
/// </summary>
public record ListQueryParams : PaginationParams
{
    /// <summary>
    /// Field to sort by
    /// </summary>
    public string? Sort { get; init; }

    /// <summary>
    /// Sort direction: "asc" or "desc"
    /// </summary>
    public string Order { get; init; } = "desc";

    /// <summary>
    /// Search term for filtering results
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Whether to sort in descending order
    /// </summary>
    public bool IsDescending => Order.Equals("desc", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Cursor-based pagination result with navigation tokens.
/// More efficient for large datasets than offset-based pagination.
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public class CursorPagedResult<T>
{
    /// <summary>
    /// The items in the current page
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>
    /// Cursor for the next page (null if no more pages)
    /// </summary>
    public string? NextCursor { get; init; }

    /// <summary>
    /// Cursor for the previous page (null if on first page)
    /// </summary>
    public string? PreviousCursor { get; init; }

    /// <summary>
    /// Whether there are more items after this page
    /// </summary>
    public bool HasMore { get; init; }

    /// <summary>
    /// Total count (optional, may be expensive to compute)
    /// </summary>
    public int? TotalCount { get; init; }
}

/// <summary>
/// Helper class for building cursor-based pagination
/// </summary>
public static class CursorPagination
{
    /// <summary>
    /// Encodes an ID as a cursor string
    /// </summary>
    public static string EncodeCursor(Guid id, DateTime? timestamp = null)
    {
        var data = timestamp.HasValue
            ? $"{id}|{timestamp.Value:O}"
            : id.ToString();
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data));
    }

    /// <summary>
    /// Decodes a cursor string to extract the ID
    /// </summary>
    public static (Guid Id, DateTime? Timestamp) DecodeCursor(string cursor)
    {
        try
        {
            var data = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = data.Split('|');
            
            var id = Guid.Parse(parts[0]);
            DateTime? timestamp = parts.Length > 1 ? DateTime.Parse(parts[1]) : null;
            
            return (id, timestamp);
        }
        catch
        {
            return (Guid.Empty, null);
        }
    }

    /// <summary>
    /// Creates a cursor-based result from a list of items
    /// </summary>
    public static CursorPagedResult<T> CreateResult<T>(
        IReadOnlyList<T> items,
        int requestedCount,
        Func<T, Guid> idSelector,
        Func<T, DateTime>? timestampSelector = null,
        int? totalCount = null)
    {
        var hasMore = items.Count > requestedCount;
        var resultItems = hasMore ? items.Take(requestedCount).ToList() : items.ToList();

        string? nextCursor = null;
        if (hasMore && resultItems.Any())
        {
            var lastItem = resultItems.Last();
            var timestamp = timestampSelector?.Invoke(lastItem);
            nextCursor = EncodeCursor(idSelector(lastItem), timestamp);
        }

        return new CursorPagedResult<T>
        {
            Items = resultItems,
            NextCursor = nextCursor,
            HasMore = hasMore,
            TotalCount = totalCount
        };
    }
}
