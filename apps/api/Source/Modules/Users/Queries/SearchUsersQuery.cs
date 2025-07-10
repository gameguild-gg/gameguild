using GameGuild.Common;


namespace GameGuild.Modules.Users;

/// <summary>
/// Query to search users with filtering and pagination
/// </summary>
public sealed class SearchUsersQuery : PaginatedQuery<User>
{
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
  public UserSortField SortBy { get; set; } = UserSortField.UpdatedAt;

  /// <summary>
  /// Sort direction
  /// </summary>
  public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}