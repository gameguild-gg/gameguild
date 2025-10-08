namespace GameGuild.Modules.Authentication;

/// <summary>
/// Service for blockchain certificate operations
/// </summary>
public interface IBlockchainCertificateService
{
    /// <summary>
    /// Anchors a certificate on the blockchain
    /// </summary>
    /// <param name="certificateHash">Hash of the certificate data</param>
    /// <param name="metadata">Certificate metadata</param>
    /// <param name="blockchainNetwork">Target blockchain network</param>
    /// <returns>Transaction hash and block number</returns>
    Task<BlockchainAnchorResult> AnchorCertificateAsync(
        string certificateHash,
        string metadata,
        string blockchainNetwork);

    /// <summary>
    /// Verifies a certificate exists on blockchain
    /// </summary>
    /// <param name="transactionHash">Transaction hash to verify</param>
    /// <param name="blockchainNetwork">Blockchain network</param>
    /// <returns>Verification result</returns>
    Task<BlockchainVerificationResult> VerifyCertificateAsync(
        string transactionHash,
        string blockchainNetwork);

    /// <summary>
    /// Retrieves certificate data from blockchain
    /// </summary>
    /// <param name="transactionHash">Transaction hash</param>
    /// <param name="blockchainNetwork">Blockchain network</param>
    /// <returns>Certificate data</returns>
    Task<string> GetCertificateDataAsync(
        string transactionHash,
        string blockchainNetwork);

    /// <summary>
    /// Creates an NFT certificate on blockchain
    /// </summary>
    /// <param name="certificateData">Certificate data</param>
    /// <param name="contractAddress">NFT contract address</param>
    /// <param name="blockchainNetwork">Blockchain network</param>
    /// <returns>Token ID and transaction hash</returns>
    Task<NftCertificateResult> CreateNftCertificateAsync(
        string certificateData,
        string contractAddress,
        string blockchainNetwork);

    /// <summary>
    /// Revokes a certificate on blockchain
    /// </summary>
    /// <param name="transactionHash">Original certificate transaction hash</param>
    /// <param name="reason">Revocation reason</param>
    /// <param name="blockchainNetwork">Blockchain network</param>
    /// <returns>Revocation transaction hash</returns>
    Task<string> RevokeCertificateAsync(
        string transactionHash,
        string reason,
        string blockchainNetwork);
}

/// <summary>
/// Result of blockchain anchoring operation
/// </summary>
public class BlockchainAnchorResult
{
    public string TransactionHash { get; set; } = string.Empty;
    public long BlockNumber { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string BlockchainNetwork { get; set; } = string.Empty;
    public decimal GasCost { get; set; }
}

/// <summary>
/// Result of blockchain verification
/// </summary>
public class BlockchainVerificationResult
{
    public bool IsValid { get; set; }
    public string CertificateHash { get; set; } = string.Empty;
    public string Metadata { get; set; } = string.Empty;
    public long BlockNumber { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of NFT certificate creation
/// </summary>
public class NftCertificateResult
{
    public string TokenId { get; set; } = string.Empty;
    public string TransactionHash { get; set; } = string.Empty;
    public string ContractAddress { get; set; } = string.Empty;
    public long BlockNumber { get; set; }
}
