using GameGuild.CQRS;


namespace GameGuild.Modules.Users;

/// <summary> Command to update user balance </summary>
public sealed class UpdateUserBalanceCommand : ICommand<User> {
  [Required] public Guid UserId { get; set; }

  [Range(0, double.MaxValue)] public decimal Balance { get; set; }

  [Range(0, double.MaxValue)] public decimal AvailableBalance { get; set; }

  public string? Reason { get; set; }

  /// <summary> Expected version for optimistic concurrency control </summary>
  public int? ExpectedVersion { get; set; }
}
