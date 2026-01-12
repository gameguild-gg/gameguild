
namespace GameGuild.Identity.Authorization;

/// <summary>
///     Factory for resolving scoped rule evaluators from DI.
///     This eliminates the hard-coded switch statement in RulesetAuthorizationHandler.
/// </summary>
public interface IScopedRuleEvaluatorFactory
{
    /// <summary>
    ///     Gets a scoped rule evaluator by its type name.
    /// </summary>
    /// <param name="ruleType">The rule type identifier.</param>
    /// <returns>The evaluator, or null if not found.</returns>
    IRuleEvaluator? GetEvaluator(string ruleType);

    /// <summary>
    ///     Gets all registered scoped rule types.
    /// </summary>
    IEnumerable<string> GetRegisteredTypes();
}

/// <summary>
///     Default implementation of scoped rule evaluator factory.
///     Resolves evaluators from the DI container dynamically.
/// </summary>
public sealed class ScopedRuleEvaluatorFactory : IScopedRuleEvaluatorFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     Mapping of rule types to their evaluator types.
    ///     This is the single source of truth for scoped evaluator registration.
    /// </summary>
    private static readonly Dictionary<string, Type> EvaluatorTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [RuleTypes.TenantMatch] = typeof(TenantMatchRuleEvaluator),
        [RuleTypes.RequireAllPermissions] = typeof(RequireAllPermissionsRuleEvaluator),
        [RuleTypes.RequireAnyPermission] = typeof(RequireAnyPermissionRuleEvaluator),
        [RuleTypes.SelfOrPermission] = typeof(SelfOrPermissionRuleEvaluator),
        [RuleTypes.OwnerOrAcl] = typeof(OwnerOrAclRuleEvaluator),
        [RuleTypes.RequireIpAllowList] = typeof(RequireIpAllowListRuleEvaluator)
    };

    public ScopedRuleEvaluatorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public IRuleEvaluator? GetEvaluator(string ruleType)
    {
        if (!EvaluatorTypes.TryGetValue(ruleType, out var evaluatorType))
        {
            return null;
        }

        return _serviceProvider.GetService(evaluatorType) as IRuleEvaluator;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRegisteredTypes() => EvaluatorTypes.Keys;

    /// <summary>
    ///     Gets all evaluator type mappings for service registration.
    /// </summary>
    public static IEnumerable<(string RuleType, Type EvaluatorType)> GetAllMappings() =>
        EvaluatorTypes.Select(kvp => (kvp.Key, kvp.Value));
}
