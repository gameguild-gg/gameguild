namespace GameGuild.Users.Models;

/// <summary>
///     User not found exception
/// </summary>
public class UserNotFoundException : Exception
{
    /// <summary>
    ///     Creates a new instance of UserNotFoundException
    /// </summary>
    public UserNotFoundException() : base("User was not found.") { }

    /// <summary>
    ///     Creates a new instance of UserNotFoundException with an email
    /// </summary>
    /// <param name="email">The email of the user that was not found</param>
    public UserNotFoundException(string email) : base($"User with email {email} was not found.") { Email = email; }

    /// <summary>
    ///     Creates a new instance of UserNotFoundException with an error message and inner exception
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    public UserNotFoundException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    ///     Creates a new instance of UserNotFoundException
    /// </summary>
    /// <param name="userId">The ID of the user that was not found</param>
    public UserNotFoundException(Guid userId) : base($"User with ID {userId} was not found.") { UserId = userId; }

    /// <summary>
    ///     The ID of the user that was not found
    /// </summary>
    public Guid? UserId { get; }

    /// <summary>
    ///     The email of the user that was not found
    /// </summary>
    public string? Email { get; }
}
