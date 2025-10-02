using GameGuild.Modules.Permissions.Abstractions;

namespace GameGuild.Modules.Permissions.Policies;

/// <summary>
/// Fluent API for building complex permission policies
/// </summary>
public class PermissionPolicyBuilder
{
    private readonly List<IPermissionPolicyRule> _rules = new();
    private TimeSpan? _cacheDuration;
    private bool _enableAuditLogging = false;
    private bool _allowDelegation = true;

    /// <summary>
    /// Require user to be authenticated
    /// </summary>
    public PermissionPolicyBuilder RequireAuthenticated()
    {
        _rules.Add(new AuthenticatedRule());
        return this;
    }

    /// <summary>
    /// Require user to be in a tenant
    /// </summary>
    public PermissionPolicyBuilder RequireTenant()
    {
        _rules.Add(new TenantRequiredRule());
        return this;
    }

    /// <summary>
    /// Require any of the specified permissions
    /// </summary>
    public PermissionPolicyBuilder RequireAnyPermission(params PermissionType[] permissions)
    {
        _rules.Add(new AnyPermissionRule(permissions));
        return this;
    }

    /// <summary>
    /// Require all of the specified permissions
    /// </summary>
    public PermissionPolicyBuilder RequireAllPermissions(params PermissionType[] permissions)
    {
        _rules.Add(new AllPermissionsRule(permissions));
        return this;
    }

    /// <summary>
    /// Require resource ownership
    /// </summary>
    public PermissionPolicyBuilder RequireOwnership()
    {
        _rules.Add(new OwnershipRule());
        return this;
    }

    /// <summary>
    /// Add OR condition for next rule
    /// </summary>
    public PermissionPolicyBuilder Or()
    {
        _rules.Add(new OrOperatorRule());
        return this;
    }

    /// <summary>
    /// Add AND condition for next rule (default behavior)
    /// </summary>
    public PermissionPolicyBuilder And()
    {
        _rules.Add(new AndOperatorRule());
        return this;
    }

    /// <summary>
    /// Enable caching for permission checks
    /// </summary>
    public PermissionPolicyBuilder WithCaching(TimeSpan duration)
    {
        _cacheDuration = duration;
        return this;
    }

    /// <summary>
    /// Enable audit logging for permission checks
    /// </summary>
    public PermissionPolicyBuilder WithAuditLogging()
    {
        _enableAuditLogging = true;
        return this;
    }

    /// <summary>
    /// Allow or disallow permission delegation
    /// </summary>
    public PermissionPolicyBuilder WithDelegation(bool allow = true)
    {
        _allowDelegation = allow;
        return this;
    }

    /// <summary>
    /// Require specific tenant tier
    /// </summary>
    public PermissionPolicyBuilder RequireTenantTier(string tier)
    {
        _rules.Add(new TenantTierRule(tier));
        return this;
    }

    /// <summary>
    /// Require user to have specific role
    /// </summary>
    public PermissionPolicyBuilder RequireRole(string role)
    {
        _rules.Add(new RoleRequiredRule(role));
        return this;
    }

    /// <summary>
    /// Add custom rule
    /// </summary>
    public PermissionPolicyBuilder AddCustomRule(IPermissionPolicyRule rule)
    {
        _rules.Add(rule);
        return this;
    }

    /// <summary>
    /// Build the permission policy
    /// </summary>
    public IPermissionPolicy Build()
    {
        return new PermissionPolicy(_rules.ToArray())
        {
            CacheDuration = _cacheDuration,
            EnableAuditLogging = _enableAuditLogging,
            AllowDelegation = _allowDelegation
        };
    }
}

/// <summary>
/// Interface for permission policy rules
/// </summary>
public interface IPermissionPolicyRule
{
    /// <summary>
    /// Evaluate the rule
    /// </summary>
    Task<bool> EvaluateAsync(PermissionPolicyContext context);

    /// <summary>
    /// Rule description for logging/debugging
    /// </summary>
    string Description { get; }
}

/// <summary>
/// Context for permission policy evaluation
/// </summary>
public class PermissionPolicyContext
{
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? ResourceId { get; set; }
    public string? ContentType { get; set; }
    public string? ResourceType { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public HttpContext? HttpContext { get; set; }
    public ICachedPermissionService PermissionService { get; set; } = null!;
    public IPermissionDelegationService DelegationService { get; set; } = null!;
}

/// <summary>
/// Interface for permission policies
/// </summary>
public interface IPermissionPolicy
{
    /// <summary>
    /// Evaluate the policy
    /// </summary>
    Task<bool> EvaluateAsync(PermissionPolicyContext context);

    /// <summary>
    /// Cache duration for policy results
    /// </summary>
    TimeSpan? CacheDuration { get; }

    /// <summary>
    /// Whether to enable audit logging
    /// </summary>
    bool EnableAuditLogging { get; }

    /// <summary>
    /// Whether to allow delegation
    /// </summary>
    bool AllowDelegation { get; }
}

/// <summary>
/// Implementation of permission policy
/// </summary>
public class PermissionPolicy : IPermissionPolicy
{
    private readonly IPermissionPolicyRule[] _rules;

    public PermissionPolicy(IPermissionPolicyRule[] rules)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    public TimeSpan? CacheDuration { get; init; }
    public bool EnableAuditLogging { get; init; }
    public bool AllowDelegation { get; init; } = true;

    public async Task<bool> EvaluateAsync(PermissionPolicyContext context)
    {
        var result = true;
        var currentOperator = LogicalOperator.And;

        foreach (var rule in _rules)
        {
            if (rule is LogicalOperatorRule operatorRule)
            {
                currentOperator = operatorRule.Operator;
                continue;
            }

            var ruleResult = await rule.EvaluateAsync(context);

            result = currentOperator switch
            {
                LogicalOperator.Or => result || ruleResult,
                LogicalOperator.And => result && ruleResult,
                _ => result && ruleResult
            };

            // Short-circuit evaluation
            if (currentOperator == LogicalOperator.And && !result)
                break;
            if (currentOperator == LogicalOperator.Or && result)
                break;
        }

        return result;
    }
}

/// <summary>
/// Logical operators for policy rules
/// </summary>
public enum LogicalOperator
{
    And,
    Or
}

/// <summary>
/// Base class for logical operator rules
/// </summary>
public abstract class LogicalOperatorRule : IPermissionPolicyRule
{
    public abstract LogicalOperator Operator { get; }
    public virtual string Description => Operator.ToString();

    public Task<bool> EvaluateAsync(PermissionPolicyContext context)
    {
        // Operator rules don't evaluate directly
        return Task.FromResult(true);
    }
}

/// <summary>
/// AND operator rule
/// </summary>
public class AndOperatorRule : LogicalOperatorRule
{
    public override LogicalOperator Operator => LogicalOperator.And;
}

/// <summary>
/// OR operator rule
/// </summary>
public class OrOperatorRule : LogicalOperatorRule
{
    public override LogicalOperator Operator => LogicalOperator.Or;
}

/// <summary>
/// Rule requiring user authentication
/// </summary>
public class AuthenticatedRule : IPermissionPolicyRule
{
    public string Description => "User must be authenticated";

    public Task<bool> EvaluateAsync(PermissionPolicyContext context)
    {
        var isAuthenticated = context.UserId.HasValue
            && context.HttpContext?.User?.Identity?.IsAuthenticated == true;
        return Task.FromResult(isAuthenticated);
    }
}

/// <summary>
/// Rule requiring tenant context
/// </summary>
public class TenantRequiredRule : IPermissionPolicyRule
{
    public string Description => "Tenant context is required";

    public Task<bool> EvaluateAsync(PermissionPolicyContext context)
    {
        return Task.FromResult(context.TenantId.HasValue);
    }
}

/// <summary>
/// Rule requiring any of specified permissions
/// </summary>
public class AnyPermissionRule : IPermissionPolicyRule
{
    private readonly PermissionType[] _permissions;

    public AnyPermissionRule(PermissionType[] permissions)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }

    public string Description => $"User must have any of: {string.Join(", ", _permissions)}";

    public async Task<bool> EvaluateAsync(PermissionPolicyContext context)
    {
        if (!context.UserId.HasValue) return false;

        foreach (var permission in _permissions)
        {
            var hasPermission = await context.PermissionService.HasTenantPermissionAsync(
                context.UserId, context.TenantId, permission);

            if (hasPermission) return true;
        }

        return false;
    }
}

/// <summary>
/// Rule requiring all specified permissions
/// </summary>
public class AllPermissionsRule : IPermissionPolicyRule
{
    private readonly PermissionType[] _permissions;

    public AllPermissionsRule(PermissionType[] permissions)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }

    public string Description => $"User must have all of: {string.Join(", ", _permissions)}";

    public async Task<bool> EvaluateAsync(PermissionPolicyContext context)
    {
        if (!context.UserId.HasValue) return false;

        foreach (var permission in _permissions)
        {
            var hasPermission = await context.PermissionService.HasTenantPermissionAsync(
                context.UserId, context.TenantId, permission);

            if (!hasPermission) return false;
        }

        return true;
    }
}

/// <summary>
/// Rule requiring resource ownership
/// </summary>
public class OwnershipRule : IPermissionPolicyRule
{
    public string Description => "User must own the resource";

    public Task<bool> EvaluateAsync(PermissionPolicyContext context)
    {
        // This would need to be implemented based on your ownership logic
        // For now, return true as placeholder
        return Task.FromResult(true);
    }
}

/// <summary>
/// Rule requiring specific tenant tier
/// </summary>
public class TenantTierRule : IPermissionPolicyRule
{
    private readonly string _requiredTier;

    public TenantTierRule(string requiredTier)
    {
        _requiredTier = requiredTier ?? throw new ArgumentNullException(nameof(requiredTier));
    }

    public string Description => $"Tenant must have tier: {_requiredTier}";

    public Task<bool> EvaluateAsync(PermissionPolicyContext context)
    {
        // This would need to be implemented based on your tenant tier logic
        // For now, return true as placeholder
        return Task.FromResult(true);
    }
}

/// <summary>
/// Rule requiring specific role
/// </summary>
public class RoleRequiredRule : IPermissionPolicyRule
{
    private readonly string _requiredRole;

    public RoleRequiredRule(string requiredRole)
    {
        _requiredRole = requiredRole ?? throw new ArgumentNullException(nameof(requiredRole));
    }

    public string Description => $"User must have role: {_requiredRole}";

    public Task<bool> EvaluateAsync(PermissionPolicyContext context)
    {
        var hasRole = context.HttpContext?.User?.IsInRole(_requiredRole) == true;
        return Task.FromResult(hasRole);
    }
}