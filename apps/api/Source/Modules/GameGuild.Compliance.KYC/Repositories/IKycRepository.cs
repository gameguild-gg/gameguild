
namespace GameGuild.Compliance.KYC;

public interface IKycRepository
{
    Task<UserKycVerification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<UserKycVerification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserKycVerification?> GetLatestVerificationAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasApprovedVerificationAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<UserKycVerification>> GetByStatusAsync(KycVerificationStatus status, CancellationToken cancellationToken = default);
    Task<UserKycVerification?> GetByExternalIdAsync(string externalVerificationId, CancellationToken cancellationToken = default);
    Task<List<UserKycVerification>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task CreateAsync(UserKycVerification verification, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserKycVerification verification, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
