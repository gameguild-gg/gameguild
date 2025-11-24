using System.Security.Cryptography;
using System.Text;
using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Permissions.Domain.Entities;
using GameGuild.Permissions.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Services;

public class AbacPolicyService(IAbacPolicyRepository repository, ILogger<AbacPolicyService> logger) : IAbacPolicyService
{
    private readonly ILogger<AbacPolicyService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IAbacPolicyRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<AbacEvaluationResult> EvaluatePoliciesAsync(AbacEvaluationContext context, CancellationToken cancellationToken = default)
    {
        // Note: Context doesn't include TenantId - we evaluate all policies or caller should provide tenant context differently
        var policies = await _repository.GetActiveByTenantAsync(null, cancellationToken);
        var result = new AbacEvaluationResult { Allowed = false, MatchedPolicyIds = new List<Guid>() };

        foreach (var policy in policies)
        {
            if (await EvaluatePolicyAsync(policy, context))
            {
                result.MatchedPolicyIds.Add(policy.Id);

                if (policy.Effect == AbacPolicyEffect.Allow) { result.Allowed = true; }
                else if (policy.Effect == AbacPolicyEffect.Deny)
                {
                    result.Allowed = false;
                    result.DenyReason = $"Denied by policy: {policy.Name}";

                    break;
                }
            }
        }

        return result;
    }

    public async Task<AbacPolicy> CreatePolicyAsync(AbacPolicy policy, CancellationToken cancellationToken = default)
    {
        policy.Enable();

        return await _repository.CreateAsync(policy, cancellationToken);
    }

    public async Task<AbacPolicy> UpdatePolicyAsync(AbacPolicy policy, CancellationToken cancellationToken = default) { return await _repository.UpdateAsync(policy, cancellationToken); }

    public async Task<bool> DeletePolicyAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(policyId, cancellationToken);

        return true;
    }

    public async Task<AbacPolicy?> GetPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default) { return await _repository.GetByIdAsync(policyId, cancellationToken); }

    public async Task<List<AbacPolicy>> GetPoliciesForTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default) { return await _repository.GetByTenantAsync(tenantId, cancellationToken); }

    public async Task<List<AbacPolicy>> GetActivePoliciesAsync(Guid? tenantId, CancellationToken cancellationToken = default) { return await _repository.GetActiveByTenantAsync(tenantId, cancellationToken); }

    public async Task<bool> EnablePolicyAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var policy = await _repository.GetByIdAsync(policyId, cancellationToken);

        if (policy == null) return false;

        policy.Enable();
        await _repository.UpdateAsync(policy, cancellationToken);

        return true;
    }

    public async Task<bool> DisablePolicyAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var policy = await _repository.GetByIdAsync(policyId, cancellationToken);

        if (policy == null) return false;

        policy.Disable();
        await _repository.UpdateAsync(policy, cancellationToken);

        return true;
    }

    public async Task<AbacEvaluationResult> TestPolicyAsync(Guid policyId, AbacEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var policy = await _repository.GetByIdAsync(policyId, cancellationToken);

        if (policy == null) throw new InvalidOperationException($"Policy {policyId} not found");

        var matches = await EvaluatePolicyAsync(policy, context);

        return new AbacEvaluationResult
        {
            Allowed = matches && policy.Effect == AbacPolicyEffect.Allow,
            MatchedPolicyIds = matches ? new List<Guid> { policyId } : new List<Guid>(),
            DenyReason = matches && policy.Effect == AbacPolicyEffect.Deny ? $"Denied by policy: {policy.Name}" : null
        };
    }

    private Task<bool> EvaluatePolicyAsync(AbacPolicy policy, AbacEvaluationContext context)
    {
        // TODO: Implement actual policy evaluation logic based on AttributeConditions
        return Task.FromResult(true);
    }
}

public class ConditionalPolicyService(IConditionalPolicyRepository repository, ILogger<ConditionalPolicyService> logger) : IConditionalPolicyService
{
    private readonly ILogger<ConditionalPolicyService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IConditionalPolicyRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<PolicyEvaluationResult> EvaluateAsync(PolicyEvaluationRequest request, CancellationToken cancellationToken = default)
    {
        // Note: PolicyEvaluationRequest doesn't have TenantId - evaluating all policies or getting from resource context
        var policies = await _repository.GetByPermissionTypeAsync(request.PermissionType, null, cancellationToken);

        foreach (var policy in policies)
        {
            if (!await EvaluateConditionsAsync(policy, request.Context))
            {
                return new PolicyEvaluationResult { Allowed = false, AppliedPolicies = new List<string> { policy.Name }, RequiredAction = policy.Action, DenyReason = $"Condition not met: {policy.Description}" };
            }
        }

        return new PolicyEvaluationResult { Allowed = true, AppliedPolicies = new List<string>() };
    }

    public async Task<ConditionalPolicy> CreatePolicyAsync(ConditionalPolicy policy, CancellationToken cancellationToken = default) { return await _repository.CreateAsync(policy, cancellationToken); }

    public async Task<ConditionalPolicy> UpdatePolicyAsync(ConditionalPolicy policy, CancellationToken cancellationToken = default) { return await _repository.UpdateAsync(policy, cancellationToken); }

    public async Task<bool> DeletePolicyAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(policyId, cancellationToken);

        return true;
    }

    public async Task<ConditionalPolicy?> GetPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default) { return await _repository.GetByIdAsync(policyId, cancellationToken); }

    public async Task<List<ConditionalPolicy>> GetActivePoliciesAsync(Guid? tenantId, CancellationToken cancellationToken = default) { return await _repository.GetActiveByTenantAsync(tenantId, cancellationToken); }

    public async Task<List<ConditionalPolicy>> GetPoliciesForPermissionAsync(string permissionType, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByPermissionTypeAsync(permissionType, tenantId, cancellationToken);
    }

    public async Task<bool> EnablePolicyAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var policy = await _repository.GetByIdAsync(policyId, cancellationToken);

        if (policy == null) return false;

        policy.Enable();
        await _repository.UpdateAsync(policy, cancellationToken);

        return true;
    }

    public async Task<bool> DisablePolicyAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var policy = await _repository.GetByIdAsync(policyId, cancellationToken);

        if (policy == null) return false;

        policy.Disable();
        await _repository.UpdateAsync(policy, cancellationToken);

        return true;
    }

    private Task<bool> EvaluateConditionsAsync(ConditionalPolicy policy, Dictionary<string, object> context)
    {
        // TODO: Implement actual condition evaluation logic
        return Task.FromResult(true);
    }
}

public class DataMaskingService(IDataMaskingRuleRepository repository, ILogger<DataMaskingService> logger) : IDataMaskingService
{
    private readonly ILogger<DataMaskingService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IDataMaskingRuleRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<DataMaskingRule> CreateRuleAsync(DataMaskingRule rule, CancellationToken cancellationToken = default) { return await _repository.CreateAsync(rule, cancellationToken); }

    public async Task<DataMaskingRule> UpdateRuleAsync(DataMaskingRule rule, CancellationToken cancellationToken = default) { return await _repository.UpdateAsync(rule, cancellationToken); }

    public async Task<bool> DeleteRuleAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(ruleId, cancellationToken);

        return true;
    }

    public async Task<DataMaskingRule?> GetRuleByIdAsync(Guid ruleId, CancellationToken cancellationToken = default) { return await _repository.GetByIdAsync(ruleId, cancellationToken); }

    public async Task<List<DataMaskingRule>> GetRulesForResourceAsync(string resourceType, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByResourceTypeAsync(resourceType, tenantId, cancellationToken);
    }

    public async Task<List<MaskingResult>> ApplyMaskingAsync(string resourceType, Dictionary<string, string> fields, Guid userId, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var rules = await _repository.GetByResourceTypeAsync(resourceType, tenantId, cancellationToken);
        var results = new List<MaskingResult>();

        foreach (var field in fields)
        {
            var rule = rules.FirstOrDefault(r => r.FieldName == field.Key && r.IsEnabled);

            if (rule != null)
            {
                var maskedValue = ApplyMaskingStrategy(field.Value, rule.MaskingType, rule.MaskCharacter);
                results.Add(new MaskingResult { FieldName = field.Key, OriginalValue = field.Value, MaskedValue = maskedValue, WasMasked = true });
            }
            else { results.Add(new MaskingResult { FieldName = field.Key, OriginalValue = field.Value, MaskedValue = field.Value, WasMasked = false }); }
        }

        return results;
    }

    public async Task<bool> ShouldMaskFieldAsync(string resourceType, string fieldName, Guid userId, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var rules = await _repository.GetByResourceTypeAsync(resourceType, tenantId, cancellationToken);

        return rules.Any(r => r.FieldName == fieldName && r.IsEnabled);
    }

    public async Task<string> MaskFieldValueAsync(string resourceType, string fieldName, string value, Guid userId, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var rules = await _repository.GetByResourceTypeAsync(resourceType, tenantId, cancellationToken);
        var rule = rules.FirstOrDefault(r => r.FieldName == fieldName && r.IsEnabled);

        if (rule == null) return value;

        return ApplyMaskingStrategy(value, rule.MaskingType, rule.MaskCharacter);
    }

    private string ApplyMaskingStrategy(string value, MaskingType strategy, char maskChar)
    {
        if (string.IsNullOrEmpty(value)) return value;

        return strategy switch
        {
            MaskingType.Full => new string(maskChar, value.Length),
            MaskingType.Partial => value.Length > 4 ? value[..2] + new string(maskChar, value.Length - 4) + value[^2..] : value,
            MaskingType.Hash => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16],
            MaskingType.PatternMask => value, // TODO: Implement pattern masking
            MaskingType.Redact => "[REDACTED]",
            MaskingType.Custom => value, // TODO: Implement custom masking
            _ => value
        };
    }
}
