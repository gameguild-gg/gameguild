namespace GameGuild.Modules.Users.Inputs;

public class SearchUsersInput {
  public string? SearchTerm { get; set; }

  public bool? IsActive { get; set; }

  public decimal? MinBalance { get; set; }

  public decimal? MaxBalance { get; set; }

  public DateTime? CreatedAfter { get; set; }

  public DateTime? CreatedBefore { get; set; }

  public DateTime? UpdatedAfter { get; set; }

  public DateTime? UpdatedBefore { get; set; }

  public bool IncludeDeleted { get; set; } = false;

  public int Skip { get; set; } = 0;

  public int Take { get; set; } = 50;

  public UserSortField SortBy { get; set; } = UserSortField.UpdatedAt;

  public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}
