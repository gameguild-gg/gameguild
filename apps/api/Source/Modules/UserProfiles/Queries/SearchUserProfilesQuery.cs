using GameGuild.CQRS;


namespace GameGuild.Modules.UserProfiles;

/// <summary> Query to search user profiles with advanced filtering and pagination </summary>
public sealed class SearchUserProfilesQuery : PaginatedQuery<UserProfile>, IQuery<Result<IEnumerable<UserProfile>>> {
  public DateTime? CreatedAfter { get; set; }

  public DateTime? CreatedBefore { get; set; }

  public DateTime? UpdatedAfter { get; set; }

  public DateTime? UpdatedBefore { get; set; }

  public string? GivenName { get; set; }

  public string? FamilyName { get; set; }

  public string? DisplayName { get; set; }

  public string? Title { get; set; }

  /// <summary> Tenant ID for multi-tenant filtering </summary>
  public Guid? TenantId { get; set; }

  /// <summary> Whether to include deleted items in the results </summary>
  public bool IncludeDeleted { get; set; }

  /// <summary> Number of items to skip for pagination </summary>
  public int Skip { get; set; }

  /// <summary> Number of items to take for pagination </summary>
  public int Take { get; set; } = 20;

  /// <summary> Sort field options </summary>
  public new UserProfileSortField SortBy { get; set; } = UserProfileSortField.UpdatedAt;

  /// <summary> Sort direction </summary>
  public new SortDirection SortDirection { get; set; } = SortDirection.Descending;
}
