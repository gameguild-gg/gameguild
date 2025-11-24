namespace GameGuild.Authentication.Models.Tokens;

/// <summary>
///     Payload extracted from a token.
/// </summary>
public class TokenPayload
{
    /// <summary>
    ///     User ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     Token type (Access, Refresh, PasswordReset, etc.).
    /// </summary>
    public string TokenType { get; set; } = string.Empty;

    /// <summary>
    ///     When the token was issued.
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    ///     When the token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    ///     Tenant context (if applicable).
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     Email address (if applicable).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    ///     User roles (if applicable).
    /// </summary>
    public string[ ]? Roles { get; set; }

    /// <summary>
    ///     Token issuer (if applicable).
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    ///     Token audience (if applicable).
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    ///     Claims included in the token.
    /// </summary>
    public Dictionary<string, object>? Claims { get; set; }

    /// <summary>
    ///     Whether the token is still valid (not expired).
    /// </summary>
    public bool IsValid { get => DateTime.UtcNow < ExpiresAt; }

    /// <summary>
    ///     Seconds until expiration.
    /// </summary>
    public int SecondsUntilExpiration { get => Math.Max(0, (int) (ExpiresAt - DateTime.UtcNow).TotalSeconds); }
}
