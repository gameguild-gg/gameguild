namespace GameGuild.Modules.Users.Inputs;

/// <summary>
/// Input for creating multiple users in a single operation
/// </summary>
public class BulkCreateUsersInput {
  /// <summary>
  /// List of users to create
  /// </summary>
  [Required]
  [MinLength(1, ErrorMessage = "At least one user must be provided")]
  [MaxLength(100, ErrorMessage = "Cannot create more than 100 users at once")]
  [GraphQLNonNullType]
  [GraphQLDescription("List of users to create")]
  public List<CreateUserInput> Users { get; set; } = new();

  /// <summary>
  /// Optional reason for the bulk operation
  /// </summary>
  [GraphQLDescription("Optional reason for the bulk operation")]
  public string? Reason { get; set; }
}
