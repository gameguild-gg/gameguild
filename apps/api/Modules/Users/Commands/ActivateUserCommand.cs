using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary> Command to activate a user </summary>
public sealed class ActivateUserCommand : ICommand<bool>
{
    [Required]
    public Guid UserId { get; init; }

    public string? Reason { get; init; }
}
