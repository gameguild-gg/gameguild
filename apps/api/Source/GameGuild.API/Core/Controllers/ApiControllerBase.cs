using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Controllers;

/// <summary>
///     Base controller for API v1 with common functionality
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase(ILogger logger) : ControllerBase
{
    protected readonly ILogger Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    ///     Creates a standardized API response
    /// </summary>
    protected ApiResponse<T> CreateResponse<T>(T data, string? message = null) { return new ApiResponse<T> { Success = true, Data = data, Message = message, Timestamp = DateTime.UtcNow }; }

    /// <summary>
    ///     Creates a standardized error response
    /// </summary>
    protected ApiResponse<object> CreateErrorResponse(string message, object? errors = null) { return new ApiResponse<object> { Success = false, Message = message, Errors = errors, Timestamp = DateTime.UtcNow }; }

    /// <summary>
    ///     Creates a paginated response
    /// </summary>
    protected PagedApiResponse<T> CreatePagedResponse<T>(IEnumerable<T> data, int page, int pageSize, int totalCount, string? message = null)
    {
        return new PagedApiResponse<T>
        {
            Success = true, Data = data, Message = message, Timestamp = DateTime.UtcNow, Page = page, PageSize = pageSize, TotalCount = totalCount, TotalPages = (int) Math.Ceiling((double) totalCount / pageSize)
        };
    }
}

/// <summary>
///     Standard API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }

    public T? Data { get; set; }

    public string? Message { get; set; }

    public object? Errors { get; set; }

    public DateTime Timestamp { get; set; }
}

/// <summary>
///     Paginated API response wrapper
/// </summary>
public class PagedApiResponse<T> : ApiResponse<IEnumerable<T>>
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public bool HasNextPage { get => Page < TotalPages; }

    public bool HasPreviousPage { get => Page > 1; }
}
