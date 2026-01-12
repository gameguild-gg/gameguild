namespace GameGuild.Identity.Users;

/// <summary>
///     User already exists exception
/// </summary>
public class UserAlreadyExistsException : Exception
{
    /// <summary>
    ///     Creates a new instance of UserAlreadyExistsException
    /// </summary>
    public UserAlreadyExistsException() : base("User already exists.") { }

    /// <summary>
    ///     Creates a new instance of UserAlreadyExistsException
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    public UserAlreadyExistsException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    ///     Creates a new instance of UserAlreadyExistsException
    /// </summary>
    /// <param name="email">The email of the user that already exists</param>
    public UserAlreadyExistsException(string email) : base($"User with email {email} already exists.") { Email = email; }

    /// <summary>
    ///     The email of the user that already exists
    /// </summary>
    public string Email { get; } = string.Empty;
}
