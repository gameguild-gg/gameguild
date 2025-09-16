using GameGuild.CQRS;


namespace GameGuild.Modules.Users;

/// <summary>
/// Query to search users with filtering and pagination
/// </summary>
public sealed class SearchUsersQuery : PaginatedQuery<User> {
  /// <summary>
  /// Whether to include soft-deleted users in results
  /// </summary>
  public bool IncludeDeleted { get; set; } = false;

  /// <summary>
  /// Number of items to skip (for pagination)
  /// </summary>
  public int Skip => (PageNumber - 1) * PageSize;

  /// <summary>
  /// Number of items to take (for pagination)
  /// </summary>
  public int Take => PageSize;

  public bool? IsActive { get; set; }

  public decimal? MinBalance { get; set; }

  public decimal? MaxBalance { get; set; }

  public DateTime? CreatedAfter { get; set; }

  public DateTime? CreatedBefore { get; set; }

  public DateTime? UpdatedAfter { get; set; }

  public DateTime? UpdatedBefore { get; set; }

  /// <summary>
  /// Sort field options
  /// </summary>
  public new UserSortField SortBy { get; set; } = UserSortField.UpdatedAt;

  /// <summary>
  /// Sort direction
  /// </summary>
  public new SortDirection SortDirection { get; set; } = SortDirection.Descending;
}
