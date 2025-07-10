namespace GameGuild.Modules.Certificates.Interfaces;

/// <summary>
/// Interface for user certificate issuance and management services
/// </summary>
public interface IUserCertificateService {
  Task<Models.UserCertificate> IssueCertificateAsync(Guid certificateId, Guid userId, Guid? programUserId = null);

  Task<Models.UserCertificate?> GetUserCertificateByIdAsync(Guid id);

  Task<Models.UserCertificate?> GetUserCertificateByCodeAsync(string verificationCode);

  Task<IEnumerable<Models.UserCertificate>> GetUserCertificatesAsync(Guid userId);

  Task<bool> VerifyCertificateAsync(string verificationCode);

  Task<bool> RevokeCertificateAsync(Guid userCertificateId, string reason);

  Task<bool> IsValidCertificateAsync(Guid userCertificateId);

  Task<IEnumerable<Models.UserCertificate>> GetExpiringCertificatesAsync(DateTime thresholdDate);

  Task<string> GenerateVerificationCodeAsync();
}
