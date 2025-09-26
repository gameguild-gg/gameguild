using GameGuild.Modules.Credentials;

namespace GameGuild.Modules.Users;

[Table("users")]
[Index(nameof(Username), IsUnique = true)]
public sealed class User : EntityBase, IUser
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Username { get; init; } = string.Empty;

    // Use EmailAddress value object for strong typing and validation
    public EmailAddress? EmailAddress { get; set; }

    // Legacy Email property for backwards compatibility (mapped to EmailAddress.Value)
    [NotMapped]
    public string Email { get => EmailAddress?.Value ?? string.Empty; set => EmailAddress = string.IsNullOrEmpty(value) ? null : new EmailAddress(value); }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Phone number as value object with validation and formatting
    /// </summary>
    public PhoneNumber? PhoneNumber { get; set; }

    // /// <summary>
    // /// Date and time when the user was last seen/logged in
    // /// </summary>
    // public DateTime? LastSeenAt { get; set; }

    // /// <summary>
    // /// Total wallet balance including pending/frozen funds
    // /// </summary>
    // public Money Balance { get; set; } = Money.Zero();

    // /// <summary>
    // /// Available balance that can be spent (excludes frozen/pending funds)
    // /// </summary>
    // public Money AvailableBalance { get; set; } = Money.Zero();

    /// <summary>
    /// Navigation property to user credentials
    /// </summary>
    public ICollection<Credential> Credentials { get; set; } = new List<Credential>();

    /// <summary>
    /// Default constructor
    /// </summary>
    public User() { }

    /// <summary>
    /// Constructor for partial initialization
    /// </summary>
    /// <param name="partial">Partial user data</param>
    public User(object partial) : base(partial) { }

    /// <summary>
    /// Activate the user
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    /// <summary>
    /// Deactivate the user
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Update user information
    /// </summary>
    /// <param name="name">New name</param>
    /// <param name="phoneNumber">New phone number</param>
    public void UpdateInfo(string name, string? phoneNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber) ? new PhoneNumber(phoneNumber) : null;
        Touch();
    }

    /// <summary>
    /// Record user activity (last seen)
    /// </summary>
    public void RecordActivity()
    {
        // LastSeenAt = DateTime.UtcNow; // TODO: Enable when LastSeenAt property is implemented
        Touch();
    }

    /// <summary>
    /// Static factory method to create a new user
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="name">User's full name</param>
    /// <param name="username">User's username</param>
    /// <param name="phoneNumber">Optional phone number</param>
    /// <returns>New User instance</returns>
    public static User Create(string email, string name, string username, string? phoneNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        return new User { Email = email, Name = name, Username = username, PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber) ? new PhoneNumber(phoneNumber) : null, IsActive = true };
    }

    /// <summary>
    /// Update the user's name
    /// </summary>
    /// <param name="name">New name</param>
    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Touch();
    }

    /// <summary>
    /// Update the user's phone number
    /// </summary>
    /// <param name="phoneNumber">New phone number</param>
    public void UpdatePhoneNumber(string? phoneNumber)
    {
        PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber) ? new PhoneNumber(phoneNumber) : null;
        Touch();
    }
}
