using GameGuild.Core;
using GameGuild.Modules.Users;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Entity for blockchain certificate anchoring (auth-adjacent trust evidence)
/// </summary>
public class BlockchainCertificateAnchor : EntityBase
{
    /// <summary>
    /// User ID associated with this certificate
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Type of certificate being anchored
    /// </summary>
    public CertificateType CertificateType { get; private set; }

    /// <summary>
    /// Blockchain network (e.g., "ethereum", "polygon", "bitcoin")
    /// </summary>
    public string BlockchainNetwork { get; private set; } = string.Empty;

    /// <summary>
    /// Transaction hash on the blockchain
    /// </summary>
    public string TransactionHash { get; private set; } = string.Empty;

    /// <summary>
    /// Block number where certificate was anchored
    /// </summary>
    public long BlockNumber { get; private set; }

    /// <summary>
    /// Hash of the certificate data (SHA-256)
    /// </summary>
    public string CertificateHash { get; private set; } = string.Empty;

    /// <summary>
    /// Certificate metadata (encrypted JSON)
    /// </summary>
    public string CertificateMetadata { get; private set; } = string.Empty;

    /// <summary>
    /// Smart contract address (if applicable)
    /// </summary>
    public string? ContractAddress { get; private set; }

    /// <summary>
    /// Token ID (for NFT certificates)
    /// </summary>
    public string? TokenId { get; private set; }

    /// <summary>
    /// When certificate was issued
    /// </summary>
    public DateTime IssuedAt { get; private set; }

    /// <summary>
    /// When certificate expires (if applicable)
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// Certificate status
    /// </summary>
    public CertificateStatus Status { get; private set; }

    /// <summary>
    /// Issuer identifier
    /// </summary>
    public string IssuerId { get; private set; } = string.Empty;

    /// <summary>
    /// When certificate was revoked (if applicable)
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Reason for revocation
    /// </summary>
    public string? RevocationReason { get; private set; }

    private BlockchainCertificateAnchor() { }

    /// <summary>
    /// Creates a new blockchain certificate anchor
    /// </summary>
    public static Result<BlockchainCertificateAnchor> Create(
        Guid userId,
        CertificateType certificateType,
        string blockchainNetwork,
        string transactionHash,
        long blockNumber,
        string certificateHash,
        string certificateMetadata,
        string issuerId,
        string? contractAddress = null,
        string? tokenId = null,
        DateTime? expiresAt = null)
    {
        if (userId == Guid.Empty)
            return Result<BlockchainCertificateAnchor>.Failure(Error.Validation(
                "BlockchainCertificateAnchor.UserId.Empty",
                "User ID cannot be empty"));

        if (string.IsNullOrWhiteSpace(blockchainNetwork))
            return Result<BlockchainCertificateAnchor>.Failure(Error.Validation(
                "BlockchainCertificateAnchor.BlockchainNetwork.Empty",
                "Blockchain network cannot be empty"));

        if (string.IsNullOrWhiteSpace(transactionHash))
            return Result<BlockchainCertificateAnchor>.Failure(Error.Validation(
                "BlockchainCertificateAnchor.TransactionHash.Empty",
                "Transaction hash cannot be empty"));

        if (string.IsNullOrWhiteSpace(certificateHash))
            return Result<BlockchainCertificateAnchor>.Failure(Error.Validation(
                "BlockchainCertificateAnchor.CertificateHash.Empty",
                "Certificate hash cannot be empty"));

        if (string.IsNullOrWhiteSpace(issuerId))
            return Result<BlockchainCertificateAnchor>.Failure(Error.Validation(
                "BlockchainCertificateAnchor.IssuerId.Empty",
                "Issuer ID cannot be empty"));

        var anchor = new BlockchainCertificateAnchor
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CertificateType = certificateType,
            BlockchainNetwork = blockchainNetwork,
            TransactionHash = transactionHash,
            BlockNumber = blockNumber,
            CertificateHash = certificateHash,
            CertificateMetadata = certificateMetadata,
            ContractAddress = contractAddress,
            TokenId = tokenId,
            IssuerId = issuerId,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            Status = CertificateStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        return Result<BlockchainCertificateAnchor>.Success(anchor);
    }

    /// <summary>
    /// Revokes the certificate
    /// </summary>
    public Result Revoke(string reason)
    {
        if (Status == CertificateStatus.Revoked)
            return Result.Failure(Error.Validation(
                "BlockchainCertificateAnchor.AlreadyRevoked",
                "Certificate is already revoked"));

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation(
                "BlockchainCertificateAnchor.RevocationReason.Empty",
                "Revocation reason is required"));

        Status = CertificateStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
        Touch();

        return Result.Success();
    }

    /// <summary>
    /// Checks if certificate is expired
    /// </summary>
    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Checks if certificate is valid (active and not expired/revoked)
    /// </summary>
    public bool IsValid() => Status == CertificateStatus.Active && !IsExpired();
}

/// <summary>
/// Types of certificates that can be anchored
/// </summary>
public enum CertificateType
{
    IdentityVerification = 1,
    Achievement = 2,
    Credential = 3,
    License = 4,
    Certification = 5,
    Attestation = 6
}

/// <summary>
/// Certificate status
/// </summary>
public enum CertificateStatus
{
    Active = 1,
    Expired = 2,
    Revoked = 3,
    Suspended = 4
}
