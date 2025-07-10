using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Users.Inputs;

public class UpdateUserBalanceInput {
  [Required] public Guid UserId { get; set; }

  [Range(0, double.MaxValue)] public decimal Balance { get; set; }

  [Range(0, double.MaxValue)] public decimal AvailableBalance { get; set; }

  public string? Reason { get; set; }

  public int? ExpectedVersion { get; set; }
}
