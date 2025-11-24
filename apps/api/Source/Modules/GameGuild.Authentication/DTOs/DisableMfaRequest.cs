using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Request to disable MFA
/// </summary>
public class DisableMfaRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
