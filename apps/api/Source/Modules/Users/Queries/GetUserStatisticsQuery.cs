namespace GameGuild.Modules.Users;

/// <summary>
/// Query to get user statistics
/// </summary>
public sealed class GetUserStatisticsQuery : IQuery<UserStatistics>
{
  public DateTime? FromDate { get; set; }
    
  public DateTime? ToDate { get; set; }
    
  public bool IncludeDeleted { get; set; } = false;
}