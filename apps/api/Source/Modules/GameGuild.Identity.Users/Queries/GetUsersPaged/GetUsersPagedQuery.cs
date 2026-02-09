using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Query to get users with pagination
/// </summary>
/// <param name="IsActive">Filter by active status (null for all users)</param>
/// <param name="PageNumber">Page number (1-based)</param>
/// <param name="PageSize">Number of users per page</param>
public sealed record GetUsersPagedQuery(bool? IsActive = null, int PageNumber = 1, int PageSize = 10) : IQuery<PagedResult<UserDto>>;
