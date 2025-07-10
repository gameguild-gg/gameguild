namespace GameGuild.Modules.Certificates;

/// <summary>
/// Interface for blockchain anchoring services
/// </summary>
public interface ICertificateBlockchainService {
  Task<CertificateBlockchainAnchor> AnchorCertificateAsync(Guid userCertificateId, string blockchainNetwork);

  Task<bool> VerifyBlockchainAnchorAsync(Guid anchorId);

  Task<CertificateBlockchainAnchor?> GetAnchorByTransactionHashAsync(string transactionHash);

  Task<IEnumerable<CertificateBlockchainAnchor>> GetAnchorsByCertificateAsync(Guid userCertificateId);

  Task<bool> UpdateAnchorStatusAsync(Guid anchorId, string status, string? transactionHash = null);
}
