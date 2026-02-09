using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Query to get users with filtering and pagination
/// </summary>
/// <param name="Email">Optional email filter</param>
/// <param name="Status">Optional status filter</param>
/// <param name="IncludeDeleted">Whether to include soft-deleted users</param>
/// <param name="SearchTerm">Optional search term</param>
/// <param name="Cursor">Optional cursor for pagination</param>
/// <param name="Limit">Number of results to return</param>
/// <param name="Sort">Sort criteria</param>
public sealed record GetUsersQuery(string? Email = null, string? Status = null, bool IncludeDeleted = false, string? SearchTerm = null, string? Cursor = null, int Limit = 50, string? Sort = null) : IQuery<PagedResult<UserDto>>;

/// <summary>
///     Query to get users metadata/statistics
/// </summary>
public sealed record GetUsersMetadataQuery : IQuery<UsersMetadataDto>;

/// <summary>
///     Metadata about users collection
/// </summary>
/// <param name="TotalCount">Total number of users</param>
/// <param name="ActiveCount">Number of active users</param>
/// <param name="InactiveCount">Number of inactive users</param>
/// <param name="DeletedCount">Number of soft-deleted users</param>
/// <param name="ETag">ETag for caching</param>
public sealed record UsersMetadataDto(int TotalCount, int ActiveCount, int InactiveCount, int DeletedCount, string ETag);
