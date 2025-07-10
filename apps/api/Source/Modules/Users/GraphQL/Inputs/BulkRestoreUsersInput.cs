using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Users.Inputs;

/// <summary>
/// Input for restoring multiple soft-deleted users in a single operation
/// </summary>
public class BulkRestoreUsersInput {
  /// <summary>
  /// List of user IDs to restore from soft delete
  /// </summary>
  [Required]
  [MinLength(1, ErrorMessage = "At least one user ID must be provided")]
  [MaxLength(100, ErrorMessage = "Cannot restore more than 100 users at once")]
  [GraphQLNonNullType]
  [GraphQLDescription("List of user IDs to restore from soft delete")]
  public List<Guid> UserIds { get; set; } = new();

  /// <summary>
  /// Optional reason for the bulk operation
  /// </summary>
  [GraphQLDescription("Optional reason for the bulk operation")]
  public string? Reason { get; set; }
}
