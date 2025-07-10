namespace GameGuild.Modules.Users.Inputs;

public class UserStatisticsInput 
{
  public DateTime? FromDate { get; set; }
  public DateTime? ToDate { get; set; }
  public bool IncludeDeleted { get; set; } = false;
}