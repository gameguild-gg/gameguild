using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>Completes email-codes enrollment with its one-time mailed proof, without an authenticator secret.</summary>
public sealed class CompleteEmailMfaSetupRequest
{
    [Required]
    [MaxLength(64)]
    [AuditSensitive]
    public string Code { get; set; } = string.Empty;
}
