using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Query to search user profiles with advanced filtering and pagination
/// </summary>
public sealed class SearchUserProfilesQuery : PaginatedQuery<UserProfile>, IQuery<Common.Result<IEnumerable<UserProfile>>> {
  public DateTime? CreatedAfter { get; set; }

  public DateTime? CreatedBefore { get; set; }

  public DateTime? UpdatedAfter { get; set; }

  public DateTime? UpdatedBefore { get; set; }

  public string? GivenName { get; set; }

  public string? FamilyName { get; set; }

  public string? DisplayName { get; set; }

  public string? Title { get; set; }

  /// <summary>
  /// Tenant ID for multi-tenant filtering
  /// </summary>
  public Guid? TenantId { get; set; }

  /// <summary>
  /// Sort field options
  /// </summary>
  public UserProfileSortField SortBy { get; set; } = UserProfileSortField.UpdatedAt;

  /// <summary>
  /// Sort direction
  /// </summary>
  public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}
