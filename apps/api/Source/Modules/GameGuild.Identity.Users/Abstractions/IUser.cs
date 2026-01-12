using GameGuild.Abstractions;

namespace GameGuild.Identity.Users;

/// <summary>
///     Interface that defines the contract for user entities in the system.
///     Extends IEntity to provide user-specific properties and operations.
/// </summary>
public interface IUser : IEntity
{
    /// <summary>
    ///     Email address of the user (unique identifier)
    /// </summary>
    string Email { get; set; }

    /// <summary>
    ///     Display name of the user
    /// </summary>
    string Name { get; set; }

    /// <summary>
    ///     Phone number of the user (optional)
    /// </summary>
    string? PhoneNumber { get; set; }

    /// <summary>
    ///     Whether this user is currently active
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    ///     Date and time when the user was last seen/logged in
    /// </summary>
    DateTime? LastSeenAt { get; set; }

    /// <summary>
    ///     Activate the user
    /// </summary>
    void Activate();

    /// <summary>
    ///     Deactivate the user
    /// </summary>
    void Deactivate();

    /// <summary>
    ///     Update user information
    /// </summary>
    /// <param name="name">New name</param>
    /// <param name="phoneNumber">New phone number</param>
    void UpdateInfo(string name, string? phoneNumber = null);

    /// <summary>
    ///     Record user activity (last seen)
    /// </summary>
    void RecordActivity();
}
