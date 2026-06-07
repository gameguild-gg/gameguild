using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request to disable MFA
/// </summary>
public class DisableMfaRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
