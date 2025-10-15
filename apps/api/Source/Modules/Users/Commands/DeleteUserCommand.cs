namespace GameGuild.Modules.Users;

/// <summary>
/// Command to delete a user
/// </summary>
public sealed class DeleteUserCommand : IRequest<bool> {
  [Required] public Guid UserId { get; set; }

  public bool SoftDelete { get; set; } = true;

  /// <summary>
  /// Reason for deletion (for audit purposes)
  /// </summary>
  public string? Reason { get; set; }
}
