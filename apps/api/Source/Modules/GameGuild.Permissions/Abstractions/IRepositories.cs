using GameGuild.Permissions.Domain.Entities;
using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Domain.Abstractions;

/// <summary>
///     Repository interface for Access Review Campaigns
/// </summary>
public interface IAccessReviewCampaignRepository
{
    Task<AccessReviewCampaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<AccessReviewCampaign>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<AccessReviewCampaign>> GetByStatusAsync(AccessReviewStatus status, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<AccessReviewCampaign>> GetExpiredCampaignsAsync(CancellationToken cancellationToken = default);

    Task<AccessReviewCampaign> CreateAsync(AccessReviewCampaign campaign, CancellationToken cancellationToken = default);

    Task<AccessReviewCampaign> UpdateAsync(AccessReviewCampaign campaign, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Access Review Items
/// </summary>
public interface IAccessReviewItemRepository
{
    Task<List<AccessReviewItem>> GetCampaignItemsAsync(Guid campaignId, CancellationToken cancellationToken = default);

    Task<List<AccessReviewItem>> GetPendingItemsByReviewerAsync(Guid reviewerId, CancellationToken cancellationToken = default);

    Task<AccessReviewItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<AccessReviewItem> UpdateItemAsync(AccessReviewItem item, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for ABAC Policies
/// </summary>
public interface IAbacPolicyRepository
{
    Task<AbacPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<AbacPolicy>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<AbacPolicy>> GetActiveByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<AbacPolicy> CreateAsync(AbacPolicy policy, CancellationToken cancellationToken = default);

    Task<AbacPolicy> UpdateAsync(AbacPolicy policy, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Conditional Policies
/// </summary>
public interface IConditionalPolicyRepository
{
    Task<ConditionalPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<ConditionalPolicy>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<ConditionalPolicy>> GetActiveByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<ConditionalPolicy>> GetByPermissionTypeAsync(string permissionType, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<ConditionalPolicy> CreateAsync(ConditionalPolicy policy, CancellationToken cancellationToken = default);

    Task<ConditionalPolicy> UpdateAsync(ConditionalPolicy policy, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Data Masking Rules
/// </summary>
public interface IDataMaskingRuleRepository
{
    Task<DataMaskingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<DataMaskingRule>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<DataMaskingRule>> GetByResourceTypeAsync(string resourceType, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<DataMaskingRule>> GetActiveByResourceTypeAsync(string resourceType, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<DataMaskingRule> CreateAsync(DataMaskingRule rule, CancellationToken cancellationToken = default);

    Task<DataMaskingRule> UpdateAsync(DataMaskingRule rule, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for JIT Elevation Requests
/// </summary>
public interface IJitElevationRequestRepository
{
    Task<JitElevationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<JitElevationRequest>> GetByRequesterAsync(Guid requesterId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<JitElevationRequest>> GetPendingRequestsAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<JitElevationRequest>> GetActiveByUserAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<JitElevationRequest>> GetExpiredElevationsAsync(CancellationToken cancellationToken = default);

    Task<JitElevationRequest> CreateAsync(JitElevationRequest request, CancellationToken cancellationToken = default);

    Task<JitElevationRequest> UpdateAsync(JitElevationRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Permission Delegations
/// </summary>
public interface IPermissionDelegationRepository
{
    Task<PermissionDelegation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<PermissionDelegation>> GetActiveByDelegateAsync(Guid delegateUserId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<PermissionDelegation>> GetByDelegatorAsync(Guid delegatorUserId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<PermissionDelegation>> GetExpiredDelegationsAsync(CancellationToken cancellationToken = default);

    Task<PermissionDelegation> CreateAsync(PermissionDelegation delegation, CancellationToken cancellationToken = default);

    Task<PermissionDelegation> UpdateAsync(PermissionDelegation delegation, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for SoD Rules
/// </summary>
public interface ISoDRuleRepository
{
    Task<SoDRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<SoDRule>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<SoDRule>> GetActiveRulesAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<SoDRule> CreateAsync(SoDRule rule, CancellationToken cancellationToken = default);

    Task<SoDRule> UpdateAsync(SoDRule rule, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for SoD Violations
/// </summary>
public interface ISoDViolationRepository
{
    Task<SoDViolation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<SoDViolation>> GetByUserAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<SoDViolation>> GetActiveViolationsAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<SoDViolation> CreateAsync(SoDViolation violation, CancellationToken cancellationToken = default);

    Task<SoDViolation> UpdateAsync(SoDViolation violation, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Delegated Admin Scopes
/// </summary>
public interface IDelegatedAdminScopeRepository
{
    Task<DelegatedAdminScope?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<DelegatedAdminScope>> GetByAdminUserAsync(Guid adminUserId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<DelegatedAdminScope>> GetActiveByAdminUserAsync(Guid adminUserId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<DelegatedAdminScope> CreateAsync(DelegatedAdminScope scope, CancellationToken cancellationToken = default);

    Task<DelegatedAdminScope> UpdateAsync(DelegatedAdminScope scope, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Permission Audit Logs
/// </summary>
public interface IPermissionAuditLogRepository
{
    Task<PermissionAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<PermissionAuditLog>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<PermissionAuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<PermissionAuditLog>> GetByUserAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<PermissionAuditLog> CreateAsync(PermissionAuditLog log, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Resource Invitations
/// </summary>
public interface IResourceInvitationRepository
{
    Task<ResourceInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<ResourceInvitation>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<ResourceInvitation>> GetPendingByResourceAsync(string resourceType, string resourceId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<ResourceInvitation>> GetByEmailAsync(string email, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<ResourceInvitation>> GetPendingByEmailAsync(string email, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<ResourceInvitation>> GetExpiredInvitationsAsync(CancellationToken cancellationToken = default);

    Task<ResourceInvitation> CreateAsync(ResourceInvitation invitation, CancellationToken cancellationToken = default);

    Task<ResourceInvitation> UpdateAsync(ResourceInvitation invitation, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Resource User Permissions
/// </summary>
public interface IResourceUserPermissionRepository
{
    Task<ResourceUserPermission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<ResourceUserPermission>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<ResourceUserPermission>> GetByResourceAsync(string resourceType, string resourceId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<ResourceUserPermission>> GetActiveByResourceAsync(string resourceType, string resourceId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<ResourceUserPermission?> GetByUserAndResourceAsync(Guid userId, string resourceType, string resourceId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<ResourceUserPermission>> GetByUserAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<ResourceUserPermission>> GetExpiredPermissionsAsync(CancellationToken cancellationToken = default);

    Task<ResourceUserPermission> CreateAsync(ResourceUserPermission permission, CancellationToken cancellationToken = default);

    Task<ResourceUserPermission> UpdateAsync(ResourceUserPermission permission, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Permission Template Versions
/// </summary>
public interface IPermissionTemplateVersionRepository
{
    Task<PermissionTemplateVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PermissionTemplateVersion?> GetVersionAsync(Guid templateId, int versionNumber, CancellationToken cancellationToken = default);

    Task<List<PermissionTemplateVersion>> GetVersionHistoryAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<PermissionTemplateVersion?> GetActiveVersionAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<PermissionTemplateVersion> CreateAsync(PermissionTemplateVersion version, CancellationToken cancellationToken = default);

    Task<PermissionTemplateVersion> UpdateAsync(PermissionTemplateVersion version, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Permission Template Migrations
/// </summary>
public interface IPermissionTemplateMigrationRepository
{
    Task<PermissionTemplateMigration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<PermissionTemplateMigration>> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<List<PermissionTemplateMigration>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);

    Task<List<PermissionTemplateMigration>> GetScheduledMigrationsAsync(DateTime before, CancellationToken cancellationToken = default);

    Task<PermissionTemplateMigration> CreateAsync(PermissionTemplateMigration migration, CancellationToken cancellationToken = default);

    Task<PermissionTemplateMigration> UpdateAsync(PermissionTemplateMigration migration, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Policy Bundles
/// </summary>
public interface IPolicyBundleRepository
{
    Task<PolicyBundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<PolicyBundle>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<PolicyBundle>> GetByTypeAsync(PolicyBundleType type, CancellationToken cancellationToken = default);

    Task<List<PolicyBundle>> GetByStatusAsync(PolicyBundleStatus status, CancellationToken cancellationToken = default);

    Task<List<PolicyBundle>> GetActiveBundlesAsync(CancellationToken cancellationToken = default);

    Task<PolicyBundle> CreateAsync(PolicyBundle bundle, CancellationToken cancellationToken = default);

    Task<PolicyBundle> UpdateAsync(PolicyBundle bundle, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Policy Bundle Deployments
/// </summary>
public interface IPolicyBundleDeploymentRepository
{
    Task<PolicyBundleDeployment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<PolicyBundleDeployment>> GetByBundleAsync(Guid bundleId, CancellationToken cancellationToken = default);

    Task<List<PolicyBundleDeployment>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<PolicyBundleDeployment>> GetByEnvironmentAsync(string environment, CancellationToken cancellationToken = default);

    Task<List<PolicyBundleDeployment>> GetActiveDeploymentsAsync(CancellationToken cancellationToken = default);

    Task<PolicyBundleDeployment> CreateAsync(PolicyBundleDeployment deployment, CancellationToken cancellationToken = default);

    Task<PolicyBundleDeployment> UpdateAsync(PolicyBundleDeployment deployment, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for Policy Registry Audit Logs
/// </summary>
public interface IPolicyRegistryAuditLogRepository
{
    Task<PolicyRegistryAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<PolicyRegistryAuditLog>> GetByBundleAsync(Guid bundleId, CancellationToken cancellationToken = default);

    Task<List<PolicyRegistryAuditLog>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<PolicyRegistryAuditLog>> GetByActionAsync(PolicyRegistryAction action, CancellationToken cancellationToken = default);

    Task<List<PolicyRegistryAuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task<PolicyRegistryAuditLog> CreateAsync(PolicyRegistryAuditLog log, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
