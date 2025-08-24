namespace GameGuild.Modules.Users;

/// <summary>
/// Command to deactivate a user
/// </summary>
public sealed class DeactivateUserCommand : IRequest<bool> {
  [Required] public Guid UserId { get; set; }

  public string? Reason { get; set; }
}
