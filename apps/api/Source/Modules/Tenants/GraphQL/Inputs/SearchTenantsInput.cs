namespace GameGuild.Modules.Tenants.Inputs;

/// <summary>
/// Input for searching tenants
/// </summary>
public class SearchTenantsInput {
  public string? SearchTerm { get; set; }

  public bool? IsActive { get; set; }

  public bool IncludeDeleted { get; set; }

  public TenantSortField SortBy { get; set; } = TenantSortField.Name;

  public bool SortDescending { get; set; }

  public int? Limit { get; set; }

  public int? Offset { get; set; }
}
