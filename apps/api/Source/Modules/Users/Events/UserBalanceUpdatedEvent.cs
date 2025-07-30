using GameGuild.Common;


namespace GameGuild.Modules.Users;

/// <summary>
/// Event raised when a user's balance is updated
/// </summary>
public sealed class UserBalanceUpdatedEvent(
  Guid userId,
  decimal previousBalance,
  decimal newBalance,
  decimal previousAvailableBalance,
  decimal newAvailableBalance,
  string? reason
)
  : DomainEventBase(userId, nameof(User)) {
  public Guid UserId { get; } = userId;

  public decimal PreviousBalance { get; } = previousBalance;

  public decimal NewBalance { get; } = newBalance;

  public decimal PreviousAvailableBalance { get; } = previousAvailableBalance;

  public decimal NewAvailableBalance { get; } = newAvailableBalance;

  public string? Reason { get; } = reason;
}
