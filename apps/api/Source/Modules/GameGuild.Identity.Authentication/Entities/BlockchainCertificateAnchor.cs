using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Represents a blockchain certificate anchor.
///     Provides immutable, verifiable record of authentication credentials and achievements.
/// </summary>
public class BlockchainCertificateAnchor
{
    /// <summary>
    ///     Unique identifier for the certificate anchor.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     The user to whom this certificate belongs.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     Type of certificate (e.g., "EmailVerified", "MfaEnabled", "AccountCreated").
    /// </summary>
    [MaxLength(128)]
    public string CertificateType { get; set; } = string.Empty;

    /// <summary>
    ///     Hash of the certificate data anchored on blockchain.
    /// </summary>
    [MaxLength(128)]
    public string CertificateHash { get; set; } = string.Empty;

    /// <summary>
    ///     The raw certificate data (may be encrypted).
    /// </summary>
    [MaxLength(4000)]
    public string CertificateData { get; set; } = string.Empty;

    /// <summary>
    ///     Blockchain transaction hash where certificate was anchored.
    /// </summary>
    [MaxLength(128)]
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    ///     Blockchain network used (e.g., "Ethereum", "Polygon").
    /// </summary>
    [MaxLength(64)]
    public string BlockchainNetwork { get; set; } = string.Empty;

    /// <summary>
    ///     Block number where transaction was included.
    /// </summary>
    public long? BlockNumber { get; set; }

    /// <summary>
    ///     When the certificate was anchored to blockchain.
    /// </summary>
    public DateTime AnchoredAt { get; set; }

    /// <summary>
    ///     Whether the certificate is still valid or has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    ///     When the certificate was revoked (if applicable).
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    ///     Reason for revocation (if applicable).
    /// </summary>
    [MaxLength(1000)]
    public string? RevocationReason { get; set; }

    /// <summary>
    ///     Transaction hash of the revocation (if applicable).
    /// </summary>
    [MaxLength(128)]
    public string? RevocationTransactionHash { get; set; }

    /// <summary>
    ///     When the certificate expires (if applicable).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     Additional metadata about the certificate (JSON).
    /// </summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    /// <summary>
    ///     Gets whether the certificate is currently valid.
    /// </summary>
    public bool IsValid
    {
        get
        {
            if (IsRevoked) { return false; }

            return !ExpiresAt.HasValue || ExpiresAt.Value > SystemClock.UtcNow;
        }
    }
}
