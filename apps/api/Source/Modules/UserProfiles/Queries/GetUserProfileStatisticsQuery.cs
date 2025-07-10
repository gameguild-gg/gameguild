using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Query to get user profile statistics
/// </summary>
public sealed class GetUserProfileStatisticsQuery : IQuery<Common.Result<UserProfileStatistics>> {
  public DateTime? FromDate { get; set; }

  public DateTime? ToDate { get; set; }

  public bool IncludeDeleted { get; set; } = false;

  public Guid? TenantId { get; set; }
}
