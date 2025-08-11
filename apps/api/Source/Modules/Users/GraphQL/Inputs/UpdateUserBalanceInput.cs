using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Users.Inputs;

/// <summary>
/// Input for updating a user's balance
/// </summary>
public class UpdateUserBalanceInput {
  /// <summary>
  /// The unique identifier of the user whose balance to update
  /// </summary>
  [Required]
  [GraphQLNonNullType]
  public Guid UserId { get; set; }

  /// <summary>
  /// The new total balance for the user
  /// </summary>
  [Range(0, double.MaxValue, ErrorMessage = "Balance must be non-negative")]
  [GraphQLDescription("The new total balance for the user")]
  public decimal Balance { get; set; }

  /// <summary>
  /// The new available balance for the user
  /// </summary>
  [Range(0, double.MaxValue, ErrorMessage = "Available balance must be non-negative")]
  [GraphQLDescription("The new available balance for the user")]
  public decimal AvailableBalance { get; set; }

  /// <summary>
  /// Optional reason for the balance update
  /// </summary>
  [GraphQLDescription("Optional reason for the balance update")]
  public string? Reason { get; set; }

  /// <summary>
  /// Expected version for optimistic concurrency control
  /// </summary>
  [GraphQLDescription("Expected version for optimistic concurrency control")]
  public int? ExpectedVersion { get; set; }
}
