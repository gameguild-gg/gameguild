
namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for anchoring authentication certificates and credentials to blockchain.
///     Provides immutable audit trail and verification of authentication events.
/// </summary>
public interface IBlockchainCertificateService
{
    /// <summary>
    ///     Anchors an authentication certificate to the blockchain.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="certificateData">The certificate data to anchor</param>
    /// <param name="certificateType">Type of certificate (email verified, MFA enabled, etc.)</param>
    /// <returns>Blockchain anchor result with transaction hash</returns>
    Task<BlockchainAnchorResult> AnchorCertificateAsync(Guid userId, string certificateData, string certificateType);

    /// <summary>
    ///     Verifies a certificate against its blockchain anchor.
    /// </summary>
    /// <param name="certificateHash">The certificate hash</param>
    /// <param name="transactionHash">The blockchain transaction hash</param>
    /// <returns>True if certificate is valid and matches blockchain record</returns>
    Task<bool> VerifyCertificateAsync(string certificateHash, string transactionHash);

    /// <summary>
    ///     Gets all blockchain-anchored certificates for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of anchored certificates</returns>
    Task<List<BlockchainCertificateAnchor>> GetUserCertificatesAsync(Guid userId);

    /// <summary>
    ///     Revokes a certificate by recording revocation on blockchain.
    /// </summary>
    /// <param name="certificateHash">The certificate hash to revoke</param>
    /// <param name="reason">Reason for revocation</param>
    /// <returns>Revocation transaction hash</returns>
    Task<string> RevokeCertificateAsync(string certificateHash, string reason);

    /// <summary>
    ///     Generates a verifiable credential in W3C standard format.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="credentialType">Type of credential</param>
    /// <param name="claims">Claims to include in the credential</param>
    /// <returns>Signed verifiable credential</returns>
    Task<VerifiableCredential> GenerateVerifiableCredentialAsync(Guid userId, string credentialType, Dictionary<string, object> claims);
}
