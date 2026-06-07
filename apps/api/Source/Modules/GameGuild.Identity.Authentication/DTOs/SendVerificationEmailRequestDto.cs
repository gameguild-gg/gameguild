using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request for sending verification email (alternative format)
/// </summary>
public class SendVerificationEmailRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? CallbackUrl { get; set; }

    public Guid? TenantId { get; set; }
}
