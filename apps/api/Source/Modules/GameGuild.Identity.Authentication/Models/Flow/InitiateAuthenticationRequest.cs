namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request to initiate an authentication flow.
/// </summary>
public abstract class InitiateAuthenticationRequest
{
    /// <summary>
    ///     Credential identifier (email, username, phone, wallet address).
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    ///     Type of credential being used.
    /// </summary>
    public string CredentialType { get; set; } = string.Empty;

    /// <summary>
    ///     Authentication method (Local, OAuth, Web3, etc.).
    /// </summary>
    public string AuthMethod { get; set; } = string.Empty;

    /// <summary>
    ///     Optional tenant context.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     IP address of the request.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    ///     User agent string.
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    ///     Optional device fingerprint.
    /// </summary>
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    ///     Additional context data.
    /// </summary>
    public Dictionary<string, object>? Context { get; set; }
}
