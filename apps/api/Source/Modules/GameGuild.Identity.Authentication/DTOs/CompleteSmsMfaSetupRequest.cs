using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>Completes SMS enrollment with its one-time phone proof, without an authenticator secret.</summary>
public sealed class CompleteSmsMfaSetupRequest
{
    [Required]
    [MaxLength(64)]
    [AuditSensitive]
    public string Code { get; set; } = string.Empty;
}
