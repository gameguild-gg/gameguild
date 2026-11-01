using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     OAuth2 client_credentials request.
/// </summary>
public class ClientCredentialsRequest
{
    [FromForm(Name = "grant_type")]
    public string GrantType { get; set; } = string.Empty;

    [FromForm(Name = "client_id")]
    public string ClientId { get; set; } = string.Empty;

    [FromForm(Name = "client_secret")]
    public string ClientSecret { get; set; } = string.Empty;

    [FromForm(Name = "scope")]
    public string? Scope { get; set; }
}

/// <summary>
///     OAuth2 token response.
/// </summary>
public class ClientCredentialsTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string? Scope { get; set; }
}

/// <summary>
///     OAuth2 error response.
/// </summary>
public class OAuth2ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? ErrorDescription { get; set; }
}

/// <summary>
///     Request to create a service account.
/// </summary>
public class CreateServiceAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TenantId { get; set; }
    public string? Scopes { get; set; }
    public string? AllowedIpAddresses { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
///     Response when a service account is created (includes secret).
/// </summary>
public class ServiceAccountCreatedResponse
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TenantId { get; set; }
    public string Scopes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Warning { get; set; } = string.Empty;
}

/// <summary>
///     Response for service account (excludes secret).
/// </summary>
public class ServiceAccountResponse
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TenantId { get; set; }
    public string Scopes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? LastAuthenticatedAt { get; set; }
    public long AuthenticationCount { get; set; }
    public int SecretRotationCount { get; set; }
}

/// <summary>
///     Response when secret is rotated.
/// </summary>
public class SecretRotationResponse
{
    public string ClientSecret { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
}

/// <summary>
///     Request to update scopes.
/// </summary>
public class UpdateScopesRequest
{
    public string Scopes { get; set; } = string.Empty;
}

/// <summary>
///     Request to partially update a service account.
/// </summary>
public class PatchServiceAccountRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Scopes { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
///     Request to lock a service account.
/// </summary>
public class LockServiceAccountRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
///     Request to bind (or clear) the X.509 client certificate used for mTLS authentication.
/// </summary>
public class BindServiceAccountCertificateRequest
{
    /// <summary>
    ///     SHA-1 thumbprint (40 hexadecimal characters, no separators) of the X.509 client
    ///     certificate. Null or empty clears the binding; the account then authenticates by
    ///     client secret only.
    /// </summary>
    [System.ComponentModel.DataAnnotations.RegularExpression("^[a-fA-F0-9]{40}$",
        ErrorMessage = "Thumbprint must be exactly 40 hexadecimal characters.")]
    public string? Thumbprint { get; set; }

    /// <summary>
    ///     SHA-256 hex hash (64 hexadecimal characters) of the certificate's Subject Public Key
    ///     Info, bound as an additional key pin. Cleared together with the thumbprint.
    /// </summary>
    [System.ComponentModel.DataAnnotations.RegularExpression("^[a-fA-F0-9]{64}$",
        ErrorMessage = "SpkiSha256 must be exactly 64 hexadecimal characters.")]
    public string? SpkiSha256 { get; set; }
}

/// <summary>
///     Response for service account audit log.
/// </summary>
public class ServiceAccountAuditLogResponse
{
    public Guid ServiceAccountId { get; set; }
    public IEnumerable<ServiceAccountAuditEntry> Entries { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
///     Single entry in service account audit log.
/// </summary>
public class ServiceAccountAuditEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PerformedBy { get; set; }
    public string? IpAddress { get; set; }
    public string? Details { get; set; }
}
