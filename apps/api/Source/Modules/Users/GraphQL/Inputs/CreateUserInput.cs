using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Users.Inputs;

/// <summary>
/// Input for creating a new user
/// </summary>
public class CreateUserInput {
  /// <summary>
  /// The user's display name
  /// </summary>
  [Required]
  [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
  [GraphQLNonNullType]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// The user's email address
  /// </summary>
  [Required]
  [EmailAddress(ErrorMessage = "Invalid email address format")]
  [StringLength(255, ErrorMessage = "Email must not exceed 255 characters")]
  [GraphQLNonNullType]
  public string Email { get; set; } = string.Empty;

  /// <summary>
  /// Whether the user account should be active upon creation
  /// </summary>
  [GraphQLDescription("Whether the user account should be active upon creation")]
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Initial balance for the user account
  /// </summary>
  [Range(0, double.MaxValue, ErrorMessage = "Initial balance must be non-negative")]
  [GraphQLDescription("Initial balance for the user account")]
  public decimal InitialBalance { get; set; } = 0;
}
