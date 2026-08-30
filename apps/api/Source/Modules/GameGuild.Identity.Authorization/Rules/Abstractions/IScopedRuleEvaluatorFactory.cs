
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

public sealed record ScopedRuleEvaluatorRegistration(string RuleType, Type EvaluatorType);

/// <summary>
///     Default implementation of scoped rule evaluator factory.
///     Resolves evaluators from the DI container dynamically.
/// </summary>
public sealed class ScopedRuleEvaluatorFactory : IScopedRuleEvaluatorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyDictionary<string, Type> _evaluatorTypes;

    private static readonly Dictionary<string, Type> BuiltInEvaluatorTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [RuleTypes.TenantMatch] = typeof(TenantMatchRuleEvaluator),
        [RuleTypes.RequireAllPermissions] = typeof(RequireAllPermissionsRuleEvaluator),
        [RuleTypes.RequireAnyPermission] = typeof(RequireAnyPermissionRuleEvaluator),
        [RuleTypes.SelfOrPermission] = typeof(SelfOrPermissionRuleEvaluator),
        [RuleTypes.OwnerOrAcl] = typeof(OwnerOrAclRuleEvaluator),
        [RuleTypes.RequireIpAllowList] = typeof(RequireIpAllowListRuleEvaluator)
    };

    public ScopedRuleEvaluatorFactory(
        IServiceProvider serviceProvider,
        IEnumerable<ScopedRuleEvaluatorRegistration>? registrations = null)
    {
        _serviceProvider = serviceProvider;
        var evaluatorTypes = new Dictionary<string, Type>(BuiltInEvaluatorTypes, StringComparer.OrdinalIgnoreCase);

        foreach (var registration in registrations ?? [])
        {
            if (evaluatorTypes.TryGetValue(registration.RuleType, out var existingType)
                && existingType != registration.EvaluatorType)
            {
                throw new InvalidOperationException(
                    $"Rule type '{registration.RuleType}' is already registered for '{existingType.FullName}'.");
            }

            evaluatorTypes[registration.RuleType] = registration.EvaluatorType;
        }

        _evaluatorTypes = evaluatorTypes;
    }

    /// <inheritdoc />
    public IRuleEvaluator? GetEvaluator(string ruleType)
    {
        if (!_evaluatorTypes.TryGetValue(ruleType, out var evaluatorType))
        {
            return null;
        }

        return _serviceProvider.GetService(evaluatorType) as IRuleEvaluator;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRegisteredTypes() => _evaluatorTypes.Keys;

    public static IEnumerable<(string RuleType, Type EvaluatorType)> GetAllMappings() =>
        BuiltInEvaluatorTypes.Select(kvp => (kvp.Key, kvp.Value));
}
