using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Query to get user profiles with pagination, search, and sorting
/// </summary>
/// <param name="Search">Optional search term to filter by display name, bio, or location</param>
/// <param name="SortBy">Field to sort by (displayName, location, createdAt, updatedAt)</param>
/// <param name="SortDirection">Sort direction (asc or desc)</param>
/// <param name="PageNumber">Page number (1-based)</param>
/// <param name="PageSize">Number of profiles per page</param>
public sealed record GetUserProfilesPagedQuery(
    string? Search = null,
    string? SortBy = null,
    string? SortDirection = "asc",
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<PagedResult<UserProfileDto>>;
