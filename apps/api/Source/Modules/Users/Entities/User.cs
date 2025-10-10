using GameGuild.Modules.Credentials;

namespace GameGuild.Modules.Users;

[Table("users")]
[Index(nameof(Username), IsUnique = true)]
public sealed class User : EntityBase, IUser
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

    // Use EmailAddress value object for strong typing and validation
    public EmailAddress? EmailAddress { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Phone number as value object with validation and formatting
    /// </summary>
    public PhoneNumber? PhoneNumber { get; set; }

    /// <summary>
    ///     Navigation property to user credentials
    /// </summary>
    public ICollection<Credential> Credentials { get; set; } = [];

    [MaxLength(100)]
    public string? GivenName { get; set; }

    [MaxLength(100)]
    public string? FamilyName { get; set; }

    /// <summary>
    ///     Full name of the user (computed from GivenName and FamilyName)
    /// </summary>
    [MaxLength(100)]
    public string? Name { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    ///     High-precision balance for financial transactions
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal Balance { get; set; } = 0m;

    /// <summary>
    ///     Available balance (not locked in pending transactions)
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal AvailableBalance { get; set; } = 0m;

    /// <summary>
    ///     Date and time when the user was last seen/logged in
    /// </summary>
    public DateTime? LastSeenAt { get; set; }

    // Legacy Email property for backwards compatibility (mapped to EmailAddress.Value)
    [NotMapped]
    public string Email { get => EmailAddress?.Value ?? string.Empty; set => EmailAddress = string.IsNullOrEmpty(value) ? null : new EmailAddress(value); }

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
    ///     Update user information
    /// </summary>
    /// <param name="givenName">New given name</param>
    /// <param name="familyName">New family name</param>
    /// <param name="phoneNumber">New phone number</param>
    public void Update(string? givenName, string? familyName, string? phoneNumber = null)
    {
        GivenName = givenName;
        FamilyName = familyName;
        PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber) ? new PhoneNumber(phoneNumber) : null;
        Touch();
    }

    /// <summary>
    ///     Update user information with full name
    /// </summary>
    /// <param name="name">New full name</param>
    /// <param name="phoneNumber">New phone number</param>
    public void UpdateInfo(string name, string? phoneNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber) ? new PhoneNumber(phoneNumber) : null;
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
    /// <param name="givenName">User's given name</param>
    /// <param name="familyName">User's family name</param>
    /// <param name="username">User's username</param>
    /// <param name="phoneNumber">Optional phone number</param>
    /// <returns>New User instance</returns>
    public static User Create(string email, string? givenName, string? familyName, string username, string? phoneNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        return new User
        {
            Email = email,
            GivenName = givenName,
            FamilyName = familyName,
            Username = username,
            PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber) ? new PhoneNumber(phoneNumber) : null,
            IsActive = true
        };
    }

    /// <summary>
    ///     Update the user's names
    /// </summary>
    /// <param name="givenName">New given name</param>
    /// <param name="familyName">New family name</param>
    public void UpdateNames(string? givenName, string? familyName)
    {
        GivenName = givenName;
        FamilyName = familyName;
        Touch();
    }

    /// <summary>
    ///     Update the user's full name
    /// </summary>
    /// <param name="name">New full name</param>
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
        PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber) ? new PhoneNumber(phoneNumber) : null;
        Touch();
    }
}
