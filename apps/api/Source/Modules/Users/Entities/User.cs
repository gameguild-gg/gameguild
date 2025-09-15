using GameGuild.Modules.Credentials;


namespace GameGuild.Modules.Users;

[Table("Users")]
[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Username), IsUnique = true)]
public sealed class User : EntityBase
{
  [Required][MaxLength(100)] public string Name { get; set; } = string.Empty;

  [Required]
  [MaxLength(50)]
  public string Username { get; set; } = string.Empty;

  [Required]
  [EmailAddress]
  [MaxLength(255)]
  public string Email { get; set; } = string.Empty;

  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Optional phone number
  /// </summary>
  [MaxLength(20)]
  public string? PhoneNumber { get; set; }

  /// <summary>
  /// Date and time when the user was last seen/logged in
  /// </summary>
  public DateTime? LastSeenAt { get; set; }

  /// <summary>
  /// Total wallet balance including pending/frozen funds
  /// </summary>
  [Column(TypeName = "decimal(10,2)")]
  public decimal Balance { get; set; }

  /// <summary>
  /// Available balance that can be spent (excludes frozen/pending funds)
  /// </summary>
  [Column(TypeName = "decimal(10,2)")]
  public decimal AvailableBalance { get; set; }

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
    PhoneNumber = phoneNumber;
    Touch();
  }

  /// <summary>
  /// Record user activity (last seen)
  /// </summary>
  public void RecordActivity()
  {
    LastSeenAt = DateTime.UtcNow;
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

    return new User
    {
      Email = email,
      Name = name,
      Username = username,
      PhoneNumber = phoneNumber,
      IsActive = true
    };
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
    PhoneNumber = phoneNumber;
    Touch();
  }
}
