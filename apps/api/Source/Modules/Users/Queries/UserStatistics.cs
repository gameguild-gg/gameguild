namespace GameGuild.Modules.Users;

/// <summary>
/// User statistics result
/// </summary>
public class UserStatistics {
  public int TotalUsers { get; set; }

  public int ActiveUsers { get; set; }

  public int InactiveUsers { get; set; }

  public int DeletedUsers { get; set; }

  public decimal TotalBalance { get; set; }

  public decimal AverageBalance { get; set; }

  public int UsersCreatedToday { get; set; }

  public int UsersCreatedThisWeek { get; set; }

  public int UsersCreatedThisMonth { get; set; }
}
