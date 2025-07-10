using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for auto-assigning a user to a group based on domain
/// </summary>
public class AutoAssignUserDto
{
    /// <summary>
    /// ID of the user to auto-assign
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Email address to use for auto-assignment logic
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Email domain to use for auto-assignment logic
    /// </summary>
    [Required]
    [EmailAddress]
    public string EmailDomain { get; set; } = string.Empty;

    /// <summary>
    /// ID of the tenant to auto-assign the user to
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
}
