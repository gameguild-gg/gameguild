using GameGuild.Common;


namespace GameGuild.Modules.Users;

/// <summary>
/// Query to get users with low balance
/// </summary>
public sealed class GetUsersWithLowBalanceQuery : PaginatedQuery<User> {
  [Range(0, double.MaxValue)] public decimal ThresholdBalance { get; set; } = 10.0m;
}
