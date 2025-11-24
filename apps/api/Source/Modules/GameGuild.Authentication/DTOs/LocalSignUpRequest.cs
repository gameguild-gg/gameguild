using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Request DTO for local sign-up
/// </summary>
public class LocalSignUpRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     Optional tenant ID to use for the sign-up. If not provided, a default tenant may be assigned
    /// </summary>
    public Guid? TenantId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }
}
