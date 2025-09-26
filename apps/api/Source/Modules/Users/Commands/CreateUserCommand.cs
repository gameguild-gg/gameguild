using System.ComponentModel.DataAnnotations;
using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary> Command for creating a new user </summary>
public sealed class CreateUserCommand : ICommand<User>
{
    /// <summary> The name of the user </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    /// <summary> The email address of the user </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    /// <summary> Whether the user is active </summary>
    public bool IsActive { get; init; } = true;
}