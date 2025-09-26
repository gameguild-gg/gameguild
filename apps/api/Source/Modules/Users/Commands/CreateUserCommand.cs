using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary> Command for creating a new user </summary>
public sealed class CreateUserCommand : ICommand<User>
{
    /// <summary> The given name of the user </summary>
    [StringLength(100)]
    public string? GivenName { get; init; }

    /// <summary> The family name of the user </summary>
    [StringLength(100)]
    public string? FamilyName { get; init; }

    /// <summary> The email address of the user </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    /// <summary> Whether the user is active </summary>
    public bool IsActive { get; init; } = true;
}
