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

// GetUsersMetadataQuery + UsersMetadataDto were removed with issue #1277 batch 1:
// the query had no handler, no route and no caller (the platform users-metadata
// rollup it anticipated never landed — docs/identity/users/cqrs-wiring-matrix.md §1,
// dead request type 11).
