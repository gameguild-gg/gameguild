using System.ComponentModel.DataAnnotations;


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

public class UpdateUserBalanceInput {
  [Required] public Guid UserId { get; set; }

  [Range(0, double.MaxValue)] public decimal Balance { get; set; }

  [Range(0, double.MaxValue)] public decimal AvailableBalance { get; set; }

  public string? Reason { get; set; }

  public int? ExpectedVersion { get; set; }
}

public class UserStatisticsInput {
  public DateTime? FromDate { get; set; }

  public DateTime? ToDate { get; set; }

  public bool IncludeDeleted { get; set; } = false;
}

public class BulkDeleteUsersInput {
  [Required] public List<Guid> UserIds { get; set; } = new();

  public bool SoftDelete { get; set; } = true;

  public string? Reason { get; set; }
}

public class BulkRestoreUsersInput {
  [Required] public List<Guid> UserIds { get; set; } = new();

  public string? Reason { get; set; }
}

public class BulkCreateUsersInput {
  [Required] public List<CreateUserInput> Users { get; set; } = new();

  public string? Reason { get; set; }
}

public class BulkActivateUsersInput {
  [Required] public List<Guid> UserIds { get; set; } = new();

  public string? Reason { get; set; }
}

public class BulkDeactivateUsersInput {
  [Required] public List<Guid> UserIds { get; set; } = new();

  public string? Reason { get; set; }
}
