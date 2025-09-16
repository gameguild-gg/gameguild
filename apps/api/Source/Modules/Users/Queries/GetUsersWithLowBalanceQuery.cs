using System.ComponentModel.DataAnnotations;
using GameGuild.CQRS;


namespace GameGuild.Modules.Users;

/// <summary>
/// Query to get users with low balance
/// </summary>
public sealed class GetUsersWithLowBalanceQuery : PaginatedQuery<User> {
  /// <summary>
  /// Whether to include soft-deleted users in results
  /// </summary>
  public bool IncludeDeleted { get; set; }

  /// <summary>
  /// Number of items to skip (for pagination)
  /// </summary>
  public int Skip => (PageNumber - 1) * PageSize;

  /// <summary>
  /// Number of items to take (for pagination)
  /// </summary>
  public int Take => PageSize;

  [Range(0, double.MaxValue)] public decimal ThresholdBalance { get; set; } = 10.0m;
}
