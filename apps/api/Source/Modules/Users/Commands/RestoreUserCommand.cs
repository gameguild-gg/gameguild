using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary> Command to restore a soft-deleted user </summary>
public sealed class RestoreUserCommand : ICommand<bool>
{
    [Required]
    public Guid UserId { get; set; }

    /// <summary> Reason for restoration (for audit purposes) </summary>
    public string? Reason { get; set; }
}
