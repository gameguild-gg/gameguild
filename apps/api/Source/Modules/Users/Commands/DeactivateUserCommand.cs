using GameGuild.CQRS;
﻿namespace GameGuild.Modules.Users;

/// <summary>
/// Command to deactivate a user
/// </summary>
public sealed class DeactivateUserCommand : ICommand<bool> {
  [Required] public Guid UserId { get; set; }

  public string? Reason { get; set; }
}
