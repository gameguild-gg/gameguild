using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request to verify MFA code
/// </summary>
public class VerifyMfaRequest
{
    public Guid UserId { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    public MfaMethod Method { get; set; } = MfaMethod.Totp;
}
