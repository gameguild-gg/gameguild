using HotChocolate;


namespace GameGuild.Modules.Users.Inputs;

/// <summary>
/// Input for searching and filtering users
/// </summary>
public class SearchUsersInput {
  /// <summary>
  /// Search term to match against user name or email
  /// </summary>
  [GraphQLDescription("Search term to match against user name or email")]
  public string? SearchTerm { get; set; }

  /// <summary>
  /// Filter by user active status
  /// </summary>
  [GraphQLDescription("Filter by user active status")]
  public bool? IsActive { get; set; }

  /// <summary>
  /// Minimum balance filter
  /// </summary>
  [GraphQLDescription("Minimum balance filter")]
  public decimal? MinBalance { get; set; }

  /// <summary>
  /// Maximum balance filter
  /// </summary>
  [GraphQLDescription("Maximum balance filter")]
  public decimal? MaxBalance { get; set; }

  /// <summary>
  /// Filter users created after this date
  /// </summary>
  [GraphQLDescription("Filter users created after this date")]
  public DateTime? CreatedAfter { get; set; }

  /// <summary>
  /// Filter users created before this date
  /// </summary>
  [GraphQLDescription("Filter users created before this date")]
  public DateTime? CreatedBefore { get; set; }

  /// <summary>
  /// Filter users updated after this date
  /// </summary>
  [GraphQLDescription("Filter users updated after this date")]
  public DateTime? UpdatedAfter { get; set; }

  /// <summary>
  /// Filter users updated before this date
  /// </summary>
  [GraphQLDescription("Filter users updated before this date")]
  public DateTime? UpdatedBefore { get; set; }

  /// <summary>
  /// Whether to include soft-deleted users in the results
  /// </summary>
  [GraphQLDescription("Whether to include soft-deleted users in the results")]
  public bool IncludeDeleted { get; set; } = false;

  /// <summary>
  /// Number of records to skip for pagination
  /// </summary>
  [GraphQLDescription("Number of records to skip for pagination")]
  public int Skip { get; set; } = 0;

  /// <summary>
  /// Maximum number of records to return
  /// </summary>
  [GraphQLDescription("Maximum number of records to return")]
  public int Take { get; set; } = 50;

  /// <summary>
  /// Field to sort results by
  /// </summary>
  [GraphQLDescription("Field to sort results by")]
  public UserSortField SortBy { get; set; } = UserSortField.UpdatedAt;

  /// <summary>
  /// Sort direction (ascending or descending)
  /// </summary>
  [GraphQLDescription("Sort direction (ascending or descending)")]
  public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}
