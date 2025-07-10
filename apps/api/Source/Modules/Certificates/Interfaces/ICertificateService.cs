namespace GameGuild.Modules.Certificates.Interfaces;

/// <summary>
/// Interface for certificate management services
/// </summary>
public interface ICertificateService {
  Task<Models.Certificate> CreateCertificateAsync(Models.Certificate certificate);

  Task<Models.Certificate?> GetCertificateByIdAsync(Guid id);

  Task<IEnumerable<Models.Certificate>> GetCertificatesByProgramAsync(Guid programId);

  Task<IEnumerable<Models.Certificate>> GetCertificatesByProductAsync(Guid productId);

  Task<Models.Certificate> UpdateCertificateAsync(Models.Certificate certificate);

  Task<bool> DeleteCertificateAsync(Guid id);

  Task<bool> CheckEligibilityAsync(Guid certificateId, Guid programUserId);

  Task<IEnumerable<Models.Certificate>> GetActiveCertificatesAsync();
}
