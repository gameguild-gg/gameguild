using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request to complete MFA setup
/// </summary>
public class CompleteMfaSetupRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;
}
