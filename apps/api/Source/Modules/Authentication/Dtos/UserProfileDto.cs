namespace GameGuild.Modules.Authentication;

/// <summary>
/// Response DTO for user profile information
/// </summary>
public class UserProfileDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Given name
    /// </summary>
    public string? GivenName { get; set; }

    /// <summary>
    /// Family name
    /// </summary>
    public string? FamilyName { get; set; }

    /// <summary>
    /// Display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// User title/position
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// User description/bio
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether email is verified
    /// </summary>
    public bool IsEmailVerified { get; set; }

    /// <summary>
    /// Account creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Current tenant information
    /// </summary>
    public TenantInfoDto? CurrentTenant { get; set; }

    /// <summary>
    /// List of available tenants for the user
    /// </summary>
    public IEnumerable<TenantInfoDto> AvailableTenants { get; set; } = new List<TenantInfoDto>();
}
