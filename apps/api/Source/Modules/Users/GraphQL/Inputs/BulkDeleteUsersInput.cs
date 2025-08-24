namespace GameGuild.Modules.Users.Inputs;

/// <summary>
/// Input for deleting multiple users in a single operation
/// </summary>
public class BulkDeleteUsersInput {
  /// <summary>
  /// List of user IDs to delete
  /// </summary>
  [Required]
  [MinLength(1, ErrorMessage = "At least one user ID must be provided")]
  [MaxLength(100, ErrorMessage = "Cannot delete more than 100 users at once")]
  [GraphQLNonNullType]
  [GraphQLDescription("List of user IDs to delete")]
  public List<Guid> UserIds { get; set; } = new();

  /// <summary>
  /// Whether to perform soft delete (recommended) or hard delete
  /// </summary>
  [GraphQLDescription("Whether to perform soft delete (recommended) or hard delete")]
  public bool SoftDelete { get; set; } = true;

  /// <summary>
  /// Optional reason for the bulk operation
  /// </summary>
  [GraphQLDescription("Optional reason for the bulk operation")]
  public string? Reason { get; set; }
}
