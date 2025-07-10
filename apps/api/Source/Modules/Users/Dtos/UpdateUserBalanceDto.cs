using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Users;

/// <summary>
/// DTO for updating user balance
/// </summary>
public class UpdateUserBalanceDto {
  [Range(0, double.MaxValue)] public decimal Balance { get; set; }

  [Range(0, double.MaxValue)] public decimal AvailableBalance { get; set; }

  public string? Reason { get; set; }

  /// <summary>
  /// Expected version for optimistic concurrency control
  /// </summary>
  public int? ExpectedVersion { get; set; }
}
