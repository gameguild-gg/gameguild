namespace GameGuild.Identity.Authorization;

/// <summary>
///     Repository interface for JIT elevation requests
/// </summary>
public interface IJitElevationRequestRepository
{
    Task<JitElevationRequest> CreateAsync(JitElevationRequest request, CancellationToken cancellationToken = default);
    Task<JitElevationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(JitElevationRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<JitElevationRequest>> GetPendingRequestsAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<JitElevationRequest>> GetByRequesterAsync(Guid requesterId, Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<JitElevationRequest>> GetActiveByUserAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<JitElevationRequest>> GetExpiredElevationsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for permission delegations
/// </summary>
public interface IPermissionDelegationRepository
{
    Task<PermissionDelegation> CreateAsync(PermissionDelegation delegation, CancellationToken cancellationToken = default);
    Task<PermissionDelegation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(PermissionDelegation delegation, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<PermissionDelegation>> GetByDelegatorAsync(Guid delegatorUserId, Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<PermissionDelegation>> GetActiveByDelegateAsync(Guid delegateUserId, Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<PermissionDelegation>> GetExpiredDelegationsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Separation of Duties rules
/// </summary>
public interface ISoDRuleRepository
{
    Task<SoDRule> CreateAsync(SoDRule rule, CancellationToken cancellationToken = default);
    Task<SoDRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SoDRule> UpdateAsync(SoDRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<SoDRule>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<SoDRule>> GetActiveRulesAsync(Guid? tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Separation of Duties violations
/// </summary>
public interface ISoDViolationRepository
{
    Task<SoDViolation> CreateAsync(SoDViolation violation, CancellationToken cancellationToken = default);
    Task<SoDViolation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(SoDViolation violation, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<SoDViolation>> GetByUserAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<SoDViolation>> GetByRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);
    Task<List<SoDViolation>> GetActiveViolationsAsync(Guid? tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Access Review Campaigns
/// </summary>
public interface IAccessReviewCampaignRepository
{
    Task<AccessReviewCampaign> CreateAsync(AccessReviewCampaign campaign, CancellationToken cancellationToken = default);
    Task<AccessReviewCampaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(AccessReviewCampaign campaign, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<AccessReviewCampaign>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<AccessReviewCampaign>> GetActiveCampaignsAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<AccessReviewCampaign>> GetPendingCampaignsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Access Review Items
/// </summary>
public interface IAccessReviewItemRepository
{
    Task<AccessReviewItem> CreateAsync(AccessReviewItem item, CancellationToken cancellationToken = default);
    Task<AccessReviewItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(AccessReviewItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<AccessReviewItem>> GetByCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<List<AccessReviewItem>> GetByReviewerAsync(Guid reviewerId, CancellationToken cancellationToken = default);
    Task<List<AccessReviewItem>> GetPendingByReviewerAsync(Guid reviewerId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Delegated Admin Scopes
/// </summary>
public interface IDelegatedAdminScopeRepository
{
    Task<DelegatedAdminScope> CreateAsync(DelegatedAdminScope scope, CancellationToken cancellationToken = default);
    Task<DelegatedAdminScope?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(DelegatedAdminScope scope, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<DelegatedAdminScope>> GetByAdminUserAsync(Guid adminUserId, Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<DelegatedAdminScope>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for ABAC Policies
/// </summary>
public interface IAbacPolicyRepository
{
    Task<AbacPolicy> CreateAsync(AbacPolicy policy, CancellationToken cancellationToken = default);
    Task<AbacPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(AbacPolicy policy, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<AbacPolicy>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<AbacPolicy>> GetActivePoliciesAsync(Guid? tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Conditional Policies
/// </summary>
public interface IConditionalPolicyRepository
{
    Task<ConditionalPolicy> CreateAsync(ConditionalPolicy policy, CancellationToken cancellationToken = default);
    Task<ConditionalPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(ConditionalPolicy policy, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ConditionalPolicy>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<ConditionalPolicy>> GetActivePoliciesAsync(Guid? tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Data Masking Rules
/// </summary>
public interface IDataMaskingRuleRepository
{
    Task<DataMaskingRule> CreateAsync(DataMaskingRule rule, CancellationToken cancellationToken = default);
    Task<DataMaskingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(DataMaskingRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<DataMaskingRule>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<DataMaskingRule>> GetActiveRulesAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<List<DataMaskingRule>> GetByResourceTypeAsync(string resourceType, Guid? tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Policy Bundles
/// </summary>
public interface IPolicyBundleRepository
{
    Task<PolicyBundle> CreateAsync(PolicyBundle bundle, CancellationToken cancellationToken = default);
    Task<PolicyBundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(PolicyBundle bundle, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<PolicyBundle>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<PolicyBundle?> GetPublishedByNameAsync(string name, Guid? tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Policy Bundle Deployments
/// </summary>
public interface IPolicyBundleDeploymentRepository
{
    Task<PolicyBundleDeployment> CreateAsync(PolicyBundleDeployment deployment, CancellationToken cancellationToken = default);
    Task<PolicyBundleDeployment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(PolicyBundleDeployment deployment, CancellationToken cancellationToken = default);
    Task<List<PolicyBundleDeployment>> GetByBundleAsync(Guid bundleId, CancellationToken cancellationToken = default);
    Task<List<PolicyBundleDeployment>> GetByEnvironmentAsync(string environment, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Permission Template Versions
/// </summary>
public interface IPermissionTemplateVersionRepository
{
    Task<PermissionTemplateVersion> CreateAsync(PermissionTemplateVersion version, CancellationToken cancellationToken = default);
    Task<PermissionTemplateVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(PermissionTemplateVersion version, CancellationToken cancellationToken = default);
    Task<List<PermissionTemplateVersion>> GetByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<PermissionTemplateVersion?> GetLatestByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Permission Template Migrations
/// </summary>
public interface IPermissionTemplateMigrationRepository
{
    Task<PermissionTemplateMigration> CreateAsync(PermissionTemplateMigration migration, CancellationToken cancellationToken = default);
    Task<PermissionTemplateMigration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(PermissionTemplateMigration migration, CancellationToken cancellationToken = default);
    Task<List<PermissionTemplateMigration>> GetByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Policy Registry Audit Logs
/// </summary>
public interface IPolicyRegistryAuditLogRepository
{
    Task<PolicyRegistryAuditLog> CreateAsync(PolicyRegistryAuditLog log, CancellationToken cancellationToken = default);
    Task<PolicyRegistryAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<PolicyRegistryAuditLog>> GetByBundleIdAsync(Guid bundleId, CancellationToken cancellationToken = default);
    Task<List<PolicyRegistryAuditLog>> GetByActorAsync(Guid actorId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
    Task<List<PolicyRegistryAuditLog>> GetByActionAsync(PolicyRegistryAction action, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
