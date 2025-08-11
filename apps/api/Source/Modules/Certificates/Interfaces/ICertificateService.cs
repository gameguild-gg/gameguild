namespace GameGuild.Modules.Certificates;

/// <summary>
/// Interface for certificate management services
/// </summary>
public interface ICertificateService {
  Task<Certificate> CreateCertificateAsync(Certificate certificate);

  Task<Certificate?> GetCertificateByIdAsync(Guid id);

  Task<IEnumerable<Certificate>> GetCertificatesByProgramAsync(Guid programId);

  Task<IEnumerable<Certificate>> GetCertificatesByProductAsync(Guid productId);

  Task<Certificate> UpdateCertificateAsync(Certificate certificate);

  Task<bool> DeleteCertificateAsync(Guid id);

  Task<bool> CheckEligibilityAsync(Guid certificateId, Guid programUserId);

  Task<IEnumerable<Certificate>> GetActiveCertificatesAsync();
}
