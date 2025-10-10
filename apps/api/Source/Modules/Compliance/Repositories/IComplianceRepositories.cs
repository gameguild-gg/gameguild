using GameGuild.Modules.Compliance.Entities;

namespace GameGuild.Modules.Compliance.Repositories;

public interface IConsentPolicyRepository
{
    Task<ConsentPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ConsentPolicy>> GetAllAsync(Guid? tenantId, bool includeInactive, CancellationToken cancellationToken = default);
    Task CreateAsync(ConsentPolicy policy, CancellationToken cancellationToken = default);
    Task UpdateAsync(ConsentPolicy policy, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IPolicyVersionRepository
{
    Task<PolicyVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<PolicyVersion>> GetByPolicyIdAsync(Guid policyId, CancellationToken cancellationToken = default);
    Task<PolicyVersion?> GetCurrentVersionAsync(Guid policyId, CancellationToken cancellationToken = default);
    Task CreateAsync(PolicyVersion version, CancellationToken cancellationToken = default);
    Task UpdateAsync(PolicyVersion version, CancellationToken cancellationToken = default);
}

public interface IUserConsentRepository
{
    Task<UserConsent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<UserConsent>> GetByUserIdAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<UserConsent>> GetByPolicyIdAsync(Guid policyId, CancellationToken cancellationToken = default);
    Task<UserConsent?> GetByUserAndPolicyAsync(Guid userId, Guid policyId, CancellationToken cancellationToken = default);
    Task CreateAsync(UserConsent consent, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserConsent consent, CancellationToken cancellationToken = default);
}

public interface IComplianceAuditRepository
{
    Task<ComplianceAudit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ComplianceAudit>> GetAuditLogAsync(
        Guid? tenantId,
        Guid? userId,
        AuditEventType? eventType,
        DateTime? startDate,
        DateTime? endDate,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task CreateAsync(ComplianceAudit audit, CancellationToken cancellationToken = default);
}
