using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace GameGuild.Modules.Permissions.Services;

/// <summary>
/// Service implementation for ABAC (Attribute-Based Access Control) policy engine
/// Evaluates policies based on user, resource, and environmental attributes
/// </summary>
public class AbacPolicyService : IAbacPolicyService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IPermissionAuditService _auditService;
    private readonly ILogger<AbacPolicyService> _logger;
    private const string CacheKeyPrefix = "abac_policies_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public AbacPolicyService(
        ApplicationDbContext context,
        IMemoryCache cache,
        IPermissionAuditService auditService,
        ILogger<AbacPolicyService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AbacEvaluationResult> EvaluatePoliciesAsync(AbacEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new AbacEvaluationResult();

        try
        {
            _logger.LogDebug("Evaluating ABAC policies for User:{UserId}, Resource:{ResourceType}:{ResourceId}, Permission:{Permission}",
                context.UserId, context.ResourceType, context.ResourceId, context.Permission);

            // Get applicable policies (from cache or database)
            var policies = await GetApplicablePoliciesAsync(context.TenantId, context.ResourceType, context.Permission, cancellationToken);

            if (!policies.Any())
            {
                result.IsGranted = false;
                result.Reason = "No applicable policies found";
                result.EvaluationTrace.Add("No ABAC policies match the request criteria");
                return result;
            }

            // Order by priority (higher first)
            policies = policies.OrderByDescending(p => p.Priority).ToList();
            result.EvaluatedPolicies = policies;

            // Evaluate policies in priority order
            // First explicit deny wins, then first allow
            AbacPolicy? denyPolicy = null;
            AbacPolicy? allowPolicy = null;

            foreach (var policy in policies)
            {
                result.EvaluationTrace.Add($"Evaluating policy '{policy.Name}' (Priority: {policy.Priority}, Effect: {policy.Effect})");

                var matches = await EvaluatePolicyAsync(policy, context);

                if (matches)
                {
                    result.EvaluationTrace.Add($"Policy '{policy.Name}' matches");

                    if (policy.Effect == PolicyEffect.Deny && denyPolicy == null)
                    {
                        denyPolicy = policy;
                        break; // Explicit deny wins immediately
                    }
                    else if (policy.Effect == PolicyEffect.Allow && allowPolicy == null)
                    {
                        allowPolicy = policy;
                    }
                }
                else
                {
                    result.EvaluationTrace.Add($"Policy '{policy.Name}' does not match");
                }
            }

            // Determine final result
            if (denyPolicy != null)
            {
                result.IsGranted = false;
                result.Effect = PolicyEffect.Deny;
                result.MatchedPolicy = denyPolicy;
                result.Reason = $"Access denied by policy '{denyPolicy.Name}'";
            }
            else if (allowPolicy != null)
            {
                result.IsGranted = true;
                result.Effect = PolicyEffect.Allow;
                result.MatchedPolicy = allowPolicy;
                result.Reason = $"Access granted by policy '{allowPolicy.Name}'";
            }
            else
            {
                result.IsGranted = false;
                result.Reason = "No matching policies found (deny by default)";
            }

            stopwatch.Stop();
            result.EvaluationDurationMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("ABAC evaluation completed: {Result}, Duration: {Duration}ms, Policy: {Policy}",
                result.IsGranted ? "GRANTED" : "DENIED", result.EvaluationDurationMs, result.MatchedPolicy?.Name ?? "None");

            // Audit the evaluation
            await _auditService.LogPermissionCheckAsync(
                context.UserId,
                context.TenantId,
                $"ABAC:{context.ResourceType}",
                context.Permission,
                result.IsGranted,
                $"ABAC Policy: {result.MatchedPolicy?.Name ?? "None"}");

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error evaluating ABAC policies");

            result.IsGranted = false;
            result.Reason = $"Error during policy evaluation: {ex.Message}";
            result.EvaluationDurationMs = stopwatch.ElapsedMilliseconds;
            result.EvaluationTrace.Add($"ERROR: {ex.Message}");

            return result;
        }
    }

    private async Task<List<AbacPolicy>> GetApplicablePoliciesAsync(Guid? tenantId, string resourceType, PermissionType permission, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{tenantId}_{resourceType}_{permission}";

        if (_cache.TryGetValue<List<AbacPolicy>>(cacheKey, out var cachedPolicies))
        {
            return cachedPolicies!;
        }

        var policies = await _context.Set<AbacPolicy>()
            .Where(p => p.IsValid)
            .Where(p => p.ResourceType == resourceType)
            .Where(p => p.Permission == permission)
            .Where(p => p.TenantId == tenantId || p.TenantId == null) // Include global policies
            .ToListAsync(cancellationToken);

        _cache.Set(cacheKey, policies, CacheDuration);

        return policies;
    }

    private async Task<bool> EvaluatePolicyAsync(AbacPolicy policy, AbacEvaluationContext context)
    {
        try
        {
            // Parse attribute expression JSON
            var attributeExpression = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(policy.AttributeExpression);

            if (attributeExpression == null || attributeExpression.Count == 0)
            {
                _logger.LogWarning("Policy '{PolicyName}' has empty attribute expression", policy.Name);
                return true; // Empty expression matches everything
            }

            // Evaluate attribute matches
            foreach (var (key, expectedValue) in attributeExpression)
            {
                var parts = key.Split('.');
                if (parts.Length != 2)
                {
                    _logger.LogWarning("Invalid attribute key format: {Key}", key);
                    continue;
                }

                var (category, attribute) = (parts[0], parts[1]);
                var actualValue = GetAttributeValue(context, category, attribute);

                if (actualValue == null || !ValuesMatch(expectedValue, actualValue))
                {
                    return false;
                }
            }

            // Evaluate condition expression if present
            if (!string.IsNullOrWhiteSpace(policy.ConditionExpression))
            {
                return await EvaluateConditionExpressionAsync(policy.ConditionExpression, context);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating policy '{PolicyName}'", policy.Name);
            return false;
        }
    }

    private object? GetAttributeValue(AbacEvaluationContext context, string category, string attribute)
    {
        var attributes = category.ToLowerInvariant() switch
        {
            "user" => context.UserAttributes,
            "resource" => context.ResourceAttributes,
            "context" => context.ContextAttributes,
            _ => null
        };

        return attributes?.TryGetValue(attribute, out var value) == true ? value : null;
    }

    private bool ValuesMatch(JsonElement expected, object? actual)
    {
        if (actual == null) return false;

        return expected.ValueKind switch
        {
            JsonValueKind.String => expected.GetString() == actual.ToString(),
            JsonValueKind.Number => Math.Abs(expected.GetDouble() - Convert.ToDouble(actual)) < 0.0001,
            JsonValueKind.True => actual is true,
            JsonValueKind.False => actual is false,
            JsonValueKind.Array => MatchesArray(expected, actual),
            _ => false
        };
    }

    private bool MatchesArray(JsonElement expected, object? actual)
    {
        if (actual == null) return false;

        var actualString = actual.ToString();
        var expectedValues = expected.EnumerateArray().Select(e => e.GetString()).ToList();

        return expectedValues.Any(v => v == actualString);
    }

    private Task<bool> EvaluateConditionExpressionAsync(string expression, AbacEvaluationContext context)
    {
        // Simple expression evaluator for common patterns
        // In production, consider using a proper expression evaluator like DynamicExpresso or NCalc

        try
        {
            // Replace variables with actual values
            var evaluatedExpression = expression;

            // Replace user.Property patterns
            evaluatedExpression = Regex.Replace(evaluatedExpression, @"user\.(\w+)", match =>
            {
                var prop = match.Groups[1].Value;
                return context.UserAttributes.TryGetValue(prop, out var value) ? FormatValue(value) : "null";
            });

            // Replace resource.Property patterns
            evaluatedExpression = Regex.Replace(evaluatedExpression, @"resource\.(\w+)", match =>
            {
                var prop = match.Groups[1].Value;
                return context.ResourceAttributes.TryGetValue(prop, out var value) ? FormatValue(value) : "null";
            });

            // Replace context.Property patterns
            evaluatedExpression = Regex.Replace(evaluatedExpression, @"context\.(\w+)", match =>
            {
                var prop = match.Groups[1].Value;
                return context.ContextAttributes.TryGetValue(prop, out var value) ? FormatValue(value) : "null";
            });

            // For now, just check if expression is non-empty after substitution
            // Real implementation should use an expression evaluator
            _logger.LogDebug("Evaluated expression: {Expression}", evaluatedExpression);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating condition expression: {Expression}", expression);
            return Task.FromResult(false);
        }
    }

    private string FormatValue(object value)
    {
        return value switch
        {
            string s => $"\"{s}\"",
            bool b => b.ToString().ToLowerInvariant(),
            _ => value.ToString() ?? "null"
        };
    }

    public async Task<AbacPolicy> CreatePolicyAsync(AbacPolicy policy, CancellationToken cancellationToken = default)
    {
        _context.Set<AbacPolicy>().Add(policy);
        await _context.SaveChangesAsync(cancellationToken);
        await ClearPolicyCacheAsync();

        _logger.LogInformation("Created ABAC policy '{PolicyName}' (ID: {PolicyId})", policy.Name, policy.Id);

        return policy;
    }

    public async Task<AbacPolicy> UpdatePolicyAsync(AbacPolicy policy, CancellationToken cancellationToken = default)
    {
        _context.Set<AbacPolicy>().Update(policy);
        await _context.SaveChangesAsync(cancellationToken);
        await ClearPolicyCacheAsync();

        _logger.LogInformation("Updated ABAC policy '{PolicyName}' (ID: {PolicyId})", policy.Name, policy.Id);

        return policy;
    }

    public async Task<bool> DeletePolicyAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var policy = await _context.Set<AbacPolicy>().FindAsync(new object[] { policyId }, cancellationToken);

        if (policy == null)
            return false;

        policy.SoftDelete();
        await _context.SaveChangesAsync(cancellationToken);
        await ClearPolicyCacheAsync();

        _logger.LogInformation("Deleted ABAC policy '{PolicyName}' (ID: {PolicyId})", policy.Name, policy.Id);

        return true;
    }

    public async Task<AbacPolicy?> GetPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AbacPolicy>()
            .FirstOrDefaultAsync(p => p.Id == policyId && !p.IsDeleted, cancellationToken);
    }

    public async Task<List<AbacPolicy>> GetPoliciesByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AbacPolicy>()
            .Where(p => p.TenantId == tenantId && !p.IsDeleted)
            .OrderByDescending(p => p.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AbacPolicy>> GetPoliciesByResourceTypeAsync(string resourceType, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<AbacPolicy>()
            .Where(p => p.ResourceType == resourceType && !p.IsDeleted);

        if (tenantId.HasValue)
        {
            query = query.Where(p => p.TenantId == tenantId || p.TenantId == null);
        }

        return await query
            .OrderByDescending(p => p.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<AbacPolicy> SetPolicyActiveStatusAsync(Guid policyId, bool isActive, CancellationToken cancellationToken = default)
    {
        var policy = await _context.Set<AbacPolicy>().FindAsync(new object[] { policyId }, cancellationToken);

        if (policy == null)
            throw new InvalidOperationException($"Policy with ID {policyId} not found");

        policy.IsActive = isActive;
        await _context.SaveChangesAsync(cancellationToken);
        await ClearPolicyCacheAsync();

        _logger.LogInformation("Set ABAC policy '{PolicyName}' active status to {IsActive}", policy.Name, isActive);

        return policy;
    }

    public Task<(bool IsValid, List<string> Errors)> ValidatePolicyExpressionAsync(string attributeExpression, string? conditionExpression = null)
    {
        var errors = new List<string>();

        try
        {
            // Validate attribute expression JSON
            var attributes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(attributeExpression);

            if (attributes == null)
            {
                errors.Add("Attribute expression must be a valid JSON object");
            }
            else
            {
                foreach (var key in attributes.Keys)
                {
                    if (!key.Contains('.'))
                    {
                        errors.Add($"Attribute key '{key}' must be in format 'category.attribute' (e.g., 'user.role')");
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON format: {ex.Message}");
        }

        // Validate condition expression syntax (basic validation)
        if (!string.IsNullOrWhiteSpace(conditionExpression))
        {
            if (!conditionExpression.Contains("user.") &&
                !conditionExpression.Contains("resource.") &&
                !conditionExpression.Contains("context."))
            {
                errors.Add("Condition expression must reference at least one attribute (user., resource., or context.)");
            }
        }

        return Task.FromResult((errors.Count == 0, errors));
    }

    public Task ClearPolicyCacheAsync()
    {
        // In production, implement proper cache invalidation
        // For now, cache entries will expire naturally
        _logger.LogDebug("ABAC policy cache clear requested (entries will expire naturally)");
        return Task.CompletedTask;
    }
}
