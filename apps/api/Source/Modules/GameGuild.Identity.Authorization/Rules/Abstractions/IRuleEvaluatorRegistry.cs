namespace GameGuild.Identity.Authorization;

/// <summary>
///     Registry for rule evaluators.
///     Provides lookup of rule evaluators by type name.
/// </summary>
public interface IRuleEvaluatorRegistry
{
    /// <summary>
    ///     Gets a rule evaluator by its type name.
    /// </summary>
    /// <param name="ruleType">The rule type identifier</param>
    /// <returns>The evaluator, or null if not found</returns>
    IRuleEvaluator? GetEvaluator(string ruleType);

    /// <summary>
    ///     Gets all registered rule types.
    /// </summary>
    IEnumerable<string> GetRegisteredTypes();
}

/// <summary>
///     Default implementation of rule evaluator registry.
/// </summary>
public sealed class RuleEvaluatorRegistry : IRuleEvaluatorRegistry
{
    private readonly Dictionary<string, IRuleEvaluator> _evaluators;

    /// <summary>
    ///     Creates a registry from a collection of evaluators.
    /// </summary>
    public RuleEvaluatorRegistry(IEnumerable<IRuleEvaluator> evaluators)
    {
        _evaluators = evaluators.ToDictionary(
            e => e.RuleType,
            e => e,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IRuleEvaluator? GetEvaluator(string ruleType) =>
        _evaluators.TryGetValue(ruleType, out var evaluator) ? evaluator : null;

    /// <inheritdoc />
    public IEnumerable<string> GetRegisteredTypes() => _evaluators.Keys;
}
