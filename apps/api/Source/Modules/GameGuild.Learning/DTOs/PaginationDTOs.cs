namespace GameGuild.Learning.DTOs;

/// <summary>
/// Common pagination request parameters
/// </summary>
public record LearningPaginationRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
    
    public int Skip => (Page - 1) * PageSize;
    public int Take => PageSize;
}

/// <summary>
/// Common paginated response wrapper
/// </summary>
/// <typeparam name="T">The type of items in the response</typeparam>
public sealed record LearningPaginatedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
    
    public static LearningPaginatedResponse<T> Empty(int page = 1, int pageSize = 20) => new()
    {
        Items = Array.Empty<T>(),
        TotalCount = 0,
        Page = page,
        PageSize = pageSize
    };
    
    public static LearningPaginatedResponse<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int page,
        int pageSize) => new()
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}

/// <summary>
/// Common filter request for learning content
/// </summary>
public sealed record LearningFilterRequest
{
    public IReadOnlyList<Guid>? CategoryIds { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public IReadOnlyList<string>? DifficultyLevels { get; init; }
    public int? MinDurationMinutes { get; init; }
    public int? MaxDurationMinutes { get; init; }
    public decimal? MinRating { get; init; }
    public bool? IsFree { get; init; }
    public string? SearchQuery { get; init; }
    public Guid? InstructorId { get; init; }
}

/// <summary>
/// Common search request for learning content
/// </summary>
public sealed record LearningSearchRequest : LearningPaginationRequest
{
    public string? Query { get; init; }
    public LearningFilterRequest? Filters { get; init; }
    public SearchScope Scope { get; init; } = SearchScope.All;
}

/// <summary>
/// Search scope for learning content
/// </summary>
public enum SearchScope
{
    All = 0,
    Courses = 1,
    LearningPaths = 2,
    Content = 3,
    Skills = 4,
    Instructors = 5
}

/// <summary>
/// Common sorting options for learning content
/// </summary>
public enum LearningSortOption
{
    Relevance = 0,
    Newest = 1,
    Oldest = 2,
    HighestRated = 3,
    MostPopular = 4,
    MostEnrollments = 5,
    ShortestDuration = 6,
    LongestDuration = 7,
    LowestPrice = 8,
    HighestPrice = 9,
    Alphabetical = 10
}
