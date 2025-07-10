namespace GameGuild.Modules.Certificates.Interfaces;

/// <summary>
/// Interface for blockchain anchoring services
/// </summary>
public interface ICertificateBlockchainService {
  Task<Models.CertificateBlockchainAnchor> AnchorCertificateAsync(Guid userCertificateId, string blockchainNetwork);

  Task<bool> VerifyBlockchainAnchorAsync(Guid anchorId);

  Task<Models.CertificateBlockchainAnchor?> GetAnchorByTransactionHashAsync(string transactionHash);

  Task<IEnumerable<Models.CertificateBlockchainAnchor>> GetAnchorsByCertificateAsync(Guid userCertificateId);

  Task<bool> UpdateAnchorStatusAsync(Guid anchorId, string status, string? transactionHash = null);
}
