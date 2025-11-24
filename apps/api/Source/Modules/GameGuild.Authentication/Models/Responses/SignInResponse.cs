namespace GameGuild.Authentication.Models.Responses;

/// <summary>
///     Response for sign-in operations
/// </summary>
public class SignInResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public int ExpiresIn { get; set; } // Seconds until expiration

    public DateTime ExpiresAt { get; set; }

    public bool RequiresMfa { get; set; }

    public string? TempToken { get; set; } // Temporary token for MFA completion

    public string? MfaToken { get; set; }

    public bool RequiresAdditionalVerification { get; set; }

    public Guid SessionId { get; set; }

    public Guid? TenantId { get; set; }
}
