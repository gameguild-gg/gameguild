using HotChocolate;


namespace GameGuild.Modules.Users.Inputs;

/// <summary>
/// Input for retrieving user statistics with optional date filtering
/// </summary>
public class UserStatisticsInput {
  /// <summary>
  /// Start date for statistics calculation (inclusive)
  /// </summary>
  [GraphQLDescription("Start date for statistics calculation (inclusive)")]
  public DateTime? FromDate { get; set; }

  /// <summary>
  /// End date for statistics calculation (inclusive)
  /// </summary>
  [GraphQLDescription("End date for statistics calculation (inclusive)")]
  public DateTime? ToDate { get; set; }

  /// <summary>
  /// Whether to include soft-deleted users in statistics
  /// </summary>
  [GraphQLDescription("Whether to include soft-deleted users in statistics")]
  public bool IncludeDeleted { get; set; } = false;
}
