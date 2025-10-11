namespace GameGuild.Modules.Certificates;

/// <summary>
/// Interface for user certificate issuance and management services
/// </summary>
public interface IUserCertificateService {
  Task<UserCertificate> IssueCertificateAsync(Guid certificateId, Guid userId, Guid? programUserId = null);

  Task<UserCertificate?> GetUserCertificateByIdAsync(Guid id);

  Task<UserCertificate?> GetUserCertificateByCodeAsync(string verificationCode);

  Task<IEnumerable<UserCertificate>> GetUserCertificatesAsync(Guid userId);

  Task<bool> VerifyCertificateAsync(string verificationCode);

  Task<bool> RevokeCertificateAsync(Guid userCertificateId, string reason);

  Task<bool> IsValidCertificateAsync(Guid userCertificateId);

  Task<IEnumerable<UserCertificate>> GetExpiringCertificatesAsync(DateTime thresholdDate);

  Task<string> GenerateVerificationCodeAsync();
}
