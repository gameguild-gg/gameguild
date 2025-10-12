namespace GameGuild.Modules.Users;

public class CreateUserRequest
{
    [StringLength(100)]
    public string? GivenName { get; set; }

    [StringLength(100)]
    public string? FamilyName { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
