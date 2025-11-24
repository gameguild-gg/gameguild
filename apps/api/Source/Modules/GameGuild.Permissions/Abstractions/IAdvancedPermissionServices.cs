using GameGuild.Permissions.Domain.Entities;
using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Domain.Abstractions;

/// <summary>
///     Service interface for Access Review and Certification
/// </summary>
public interface IAccessReviewService
{
    Task<AccessReviewCampaign> CreateCampaignAsync(AccessReviewCampaign campaign, CancellationToken cancellationToken = default);

    Task<AccessReviewCampaign> UpdateCampaignAsync(AccessReviewCampaign campaign, CancellationToken cancellationToken = default);

    Task<AccessReviewCampaign> GetCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);

    Task<List<AccessReviewCampaign>> ListCampaignsAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<bool> StartCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);

    Task<bool> CompleteCampaignAsync(Guid campaignId, Guid completedBy, CancellationToken cancellationToken = default);

    Task<bool> CancelCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);

    Task<AccessReviewItem> ReviewItemAsync(Guid itemId, Guid reviewerId, AccessReviewDecision decision, string? reason, CancellationToken cancellationToken = default);

    Task<List<AccessReviewItem>> GetPendingReviewsAsync(Guid reviewerId, CancellationToken cancellationToken = default);

    Task<List<AccessReviewItem>> GetCampaignItemsAsync(Guid campaignId, CancellationToken cancellationToken = default);

    Task<bool> SendRemindersAsync(Guid campaignId, CancellationToken cancellationToken = default);

    Task<CampaignStatistics> GetStatisticsAsync(Guid campaignId, CancellationToken cancellationToken = default);

    Task<int> ProcessExpiredCampaignsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Service interface for ABAC (Attribute-Based Access Control) policies
/// </summary>
public interface IAbacPolicyService
{
    Task<AbacEvaluationResult> EvaluatePoliciesAsync(AbacEvaluationContext context, CancellationToken cancellationToken = default);

    Task<AbacPolicy> CreatePolicyAsync(AbacPolicy policy, CancellationToken cancellationToken = default);

    Task<AbacPolicy> UpdatePolicyAsync(AbacPolicy policy, CancellationToken cancellationToken = default);

    Task<bool> DeletePolicyAsync(Guid policyId, CancellationToken cancellationToken = default);

    Task<AbacPolicy?> GetPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default);

    Task<List<AbacPolicy>> GetPoliciesForTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<AbacPolicy>> GetActivePoliciesAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<bool> EnablePolicyAsync(Guid policyId, CancellationToken cancellationToken = default);

    Task<bool> DisablePolicyAsync(Guid policyId, CancellationToken cancellationToken = default);

    Task<AbacEvaluationResult> TestPolicyAsync(Guid policyId, AbacEvaluationContext context, CancellationToken cancellationToken = default);
}

/// <summary>
///     Service interface for Conditional Policies
/// </summary>
public interface IConditionalPolicyService
{
    Task<PolicyEvaluationResult> EvaluateAsync(PolicyEvaluationRequest request, CancellationToken cancellationToken = default);

    Task<ConditionalPolicy> CreatePolicyAsync(ConditionalPolicy policy, CancellationToken cancellationToken = default);

    Task<ConditionalPolicy> UpdatePolicyAsync(ConditionalPolicy policy, CancellationToken cancellationToken = default);

    Task<bool> DeletePolicyAsync(Guid policyId, CancellationToken cancellationToken = default);

    Task<ConditionalPolicy?> GetPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default);

    Task<List<ConditionalPolicy>> GetActivePoliciesAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<ConditionalPolicy>> GetPoliciesForPermissionAsync(string permissionType, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<bool> EnablePolicyAsync(Guid policyId, CancellationToken cancellationToken = default);

    Task<bool> DisablePolicyAsync(Guid policyId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Service interface for Data Masking
/// </summary>
public interface IDataMaskingService
{
    Task<DataMaskingRule> CreateRuleAsync(DataMaskingRule rule, CancellationToken cancellationToken = default);

    Task<DataMaskingRule> UpdateRuleAsync(DataMaskingRule rule, CancellationToken cancellationToken = default);

    Task<bool> DeleteRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<DataMaskingRule?> GetRuleByIdAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<List<DataMaskingRule>> GetRulesForResourceAsync(string resourceType, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<MaskingResult>> ApplyMaskingAsync(string resourceType, Dictionary<string, string> fields, Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<bool> ShouldMaskFieldAsync(string resourceType, string fieldName, Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<string> MaskFieldValueAsync(string resourceType, string fieldName, string value, Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);
}
