using System.ComponentModel.DataAnnotations;
using HotChocolate;


namespace GameGuild.Modules.Users.Inputs;

/// <summary>
/// Input for updating an existing user
/// </summary>
public class UpdateUserInput {
  /// <summary>
  /// The unique identifier of the user to update
  /// </summary>
  [Required]
  [GraphQLNonNullType]
  public Guid Id { get; set; }

  /// <summary>
  /// The user's display name
  /// </summary>
  [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
  [GraphQLDescription("The user's display name")]
  public string? Name { get; set; }

  /// <summary>
  /// The user's email address
  /// </summary>
  [EmailAddress(ErrorMessage = "Invalid email address format")]
  [StringLength(255, ErrorMessage = "Email must not exceed 255 characters")]
  [GraphQLDescription("The user's email address")]
  public string? Email { get; set; }

  /// <summary>
  /// Whether the user account should be active
  /// </summary>
  [GraphQLDescription("Whether the user account should be active")]
  public bool? IsActive { get; set; }

  /// <summary>
  /// Expected version for optimistic concurrency control
  /// </summary>
  [GraphQLDescription("Expected version for optimistic concurrency control")]
  public int? ExpectedVersion { get; set; }
}
