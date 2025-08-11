using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Users.Inputs;

/// <summary>
/// Input for deactivating multiple users in a single operation
/// </summary>
public class BulkDeactivateUsersInput {
  /// <summary>
  /// List of user IDs to deactivate
  /// </summary>
  [Required]
  [MinLength(1, ErrorMessage = "At least one user ID must be provided")]
  [MaxLength(100, ErrorMessage = "Cannot deactivate more than 100 users at once")]
  [GraphQLNonNullType]
  [GraphQLDescription("List of user IDs to deactivate")]
  public List<Guid> UserIds { get; set; } = new();

  /// <summary>
  /// Optional reason for the bulk operation
  /// </summary>
  [GraphQLDescription("Optional reason for the bulk operation")]
  public string? Reason { get; set; }
}
