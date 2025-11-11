using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Users.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Users.Entities;

/// <summary>
///     Represents a user in the system
///     Inherits from EntityBase to provide GUID IDs, version control, timestamps, and soft delete functionality
/// </summary>
[Table("Users")]
[Index(nameof(Email), IsUnique = true)]
public class User : EntityBase, IUser
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public User() { }

    /// <summary>
    ///     Constructor for partial initialization
    /// </summary>
    /// <param name="partial">Partial user data</param>
    public User(object partial) : base(partial) { }

    /// <summary>
    ///     Email address of the user (unique)
    /// </summary>
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    ///     Full name of the user
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Whether this user is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Whether this user is suspended
    /// </summary>
    public bool IsSuspended { get; set; }

    /// <summary>
    ///     Optional phone number
    /// </summary>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    ///     Date and time when the user was last seen/logged in
    /// </summary>
    public DateTime? LastSeenAt { get; set; }

    /// <summary>
    ///     Activate the user
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    /// <summary>
    ///     Deactivate the user
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    /// <summary>
    ///     Suspend the user
    /// </summary>
    public void Suspend()
    {
        IsSuspended = true;
        Touch();
    }

    /// <summary>
    ///     Unsuspend the user
    /// </summary>
    public void Unsuspend()
    {
        IsSuspended = false;
        Touch();
    }

    /// <summary>
    ///     Update user information
    /// </summary>
    /// <param name="name">New name</param>
    /// <param name="phoneNumber">New phone number</param>
    public void UpdateInfo(string name, string? phoneNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        PhoneNumber = phoneNumber;
        Touch();
    }

    /// <summary>
    ///     Record user activity (last seen)
    /// </summary>
    public void RecordActivity()
    {
        LastSeenAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    ///     Static factory method to create a new user
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="name">User's full name</param>
    /// <param name="phoneNumber">Optional phone number</param>
    /// <returns>New User instance</returns>
    public static User Create(string email, string name, string? phoneNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new User { Email = email, Name = name, PhoneNumber = phoneNumber, IsActive = true };
    }

    /// <summary>
    ///     Update the user's name
    /// </summary>
    /// <param name="name">New name</param>
    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Touch();
    }

    /// <summary>
    ///     Update the user's phone number
    /// </summary>
    /// <param name="phoneNumber">New phone number</param>
    public void UpdatePhoneNumber(string? phoneNumber)
    {
        PhoneNumber = phoneNumber;
        Touch();
    }
}
