using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Query to get user notifications with pagination, search, and filtering
/// </summary>
/// <param name="UserId">User identifier</param>
/// <param name="Search">Optional search term to filter by title or content</param>
/// <param name="SortBy">Field to sort by (createdAt, priority, type)</param>
/// <param name="SortDirection">Sort direction (asc or desc)</param>
/// <param name="IsRead">Filter by read status</param>
/// <param name="IsArchived">Filter by archived status</param>
/// <param name="Type">Filter by notification type</param>
/// <param name="Priority">Filter by priority level</param>
/// <param name="FromDate">Filter by creation date from</param>
/// <param name="ToDate">Filter by creation date to</param>
/// <param name="PageNumber">Page number (1-based)</param>
/// <param name="PageSize">Number of notifications per page</param>
public record GetUserNotificationsPagedQuery(
    Guid UserId,
    string? Search = null,
    string? SortBy = null,
    string? SortDirection = "desc",
    bool? IsRead = null,
    bool? IsArchived = null,
    string? Type = null,
    string? Priority = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<Models.PagedResult<UserNotificationDto>>;
