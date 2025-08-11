using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Users;

/// <summary>
/// DTO for updating user balance
/// </summary>
public class UpdateUserBalanceDto(decimal balance, decimal availableBalance, int? expectedVersion, string? reason) {
  [Range(0, double.MaxValue)] public decimal Balance { get; set; } = balance;

  [Range(0, double.MaxValue)] public decimal AvailableBalance { get; set; } = availableBalance;

  public string? Reason { get; set; } = reason;

  /// <summary>
  /// Expected version for optimistic concurrency control
  /// </summary>
  public int? ExpectedVersion { get; set; } = expectedVersion;
}
