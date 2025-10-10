using GameGuild.Modules.Tenants.Entities;

namespace GameGuild.Modules.Tenants.Repositories;

public interface ITenantArchivalRepository
{
    // Policies
    Task<TenantArchivalPolicy> CreatePolicyAsync(TenantArchivalPolicy policy, CancellationToken cancellationToken = default);
    Task<TenantArchivalPolicy> UpdatePolicyAsync(TenantArchivalPolicy policy, CancellationToken cancellationToken = default);
    Task<TenantArchivalPolicy?> GetPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default);
    Task<TenantArchivalPolicy?> GetPolicyByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<TenantArchivalPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);
    Task DeletePolicyAsync(Guid policyId, CancellationToken cancellationToken = default);

    // Archive Records
    Task<TenantArchiveRecord> CreateArchiveRecordAsync(TenantArchiveRecord record, CancellationToken cancellationToken = default);
    Task<TenantArchiveRecord> UpdateArchiveRecordAsync(TenantArchiveRecord record, CancellationToken cancellationToken = default);
    Task<TenantArchiveRecord?> GetArchiveRecordByIdAsync(Guid recordId, CancellationToken cancellationToken = default);
    Task<TenantArchiveRecord?> GetArchiveRecordByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<TenantArchiveRecord>> GetArchiveRecordsByStatusAsync(TenantArchivalStatus status, CancellationToken cancellationToken = default);
}
