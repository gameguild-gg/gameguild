namespace GameGuild.Modules.Users;

public class UpdateUserRequest
{
    [StringLength(100)]
    public string? GivenName { get; set; }

    [StringLength(100)]
    public string? FamilyName { get; set; }

    [StringLength(50, MinimumLength = 1)]
    public string? Username { get; set; }

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    public bool? IsActive { get; set; }

    /// <summary> Expected version for optimistic concurrency control </summary>
    public int? ExpectedVersion { get; set; }
}
