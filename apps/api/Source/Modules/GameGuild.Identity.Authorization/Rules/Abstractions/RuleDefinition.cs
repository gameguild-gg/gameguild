using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     A rule definition stored in the database.
///     Rules are ordered and evaluated sequentially within a policy.
/// </summary>
public sealed class RuleDefinition
{
    /// <summary>
    ///     The rule type identifier (matches IRuleEvaluator.RuleType).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    ///     Optional description for documentation/UI purposes.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     Rule parameters as a dictionary.
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, JsonElement>? Params { get; init; }

    [JsonPropertyName("rules")]
    public IReadOnlyList<RuleDefinition>? Rules { get; init; }

    /// <summary>
    ///     Whether this rule is enabled (allows temporarily disabling rules).
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    ///     Gets the parameters as a RuleParameters object.
    /// </summary>
    public RuleParameters GetParameters() =>
        Params is null ? new RuleParameters() : new RuleParameters(Params);

    /// <summary>
    ///     Validates this rule definition.
    /// </summary>
    /// <returns>A validation result with any errors.</returns>
    public RuleValidationResult Validate()
    {
        var errors = new List<string>();

        // Validate Type is not null or empty
        if (string.IsNullOrWhiteSpace(Type))
        {
            errors.Add("Rule type is required and cannot be empty");
        }
        else if (!RuleTypes.IsValid(Type))
        {
            errors.Add($"Unknown rule type: '{Type}'. Valid types: {string.Join(", ", RuleTypes.All)}");
        }
        else
        {
            // Validate required parameters for the rule type
            var requiredParams = RuleTypes.GetRequiredParameters(Type);
            var parameters = GetParameters();

            foreach (var param in requiredParams)
            {
                if (!parameters.HasParameter(param))
                {
                    errors.Add($"Rule type '{Type}' requires parameter '{param}'");
                }
            }

            if ((string.Equals(Type, RuleTypes.RequireAllPermissions, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(Type, RuleTypes.RequireAnyPermission, StringComparison.OrdinalIgnoreCase))
                && parameters.GetStringArray("permissions").Count == 0)
            {
                errors.Add($"Rule type '{Type}' requires at least one permission");
            }

            if (string.Equals(Type, RuleTypes.AnyOf, StringComparison.OrdinalIgnoreCase))
            {
                var enabledRules = Rules?.Where(rule => rule.Enabled).ToList() ?? [];
                if (enabledRules.Count == 0)
                {
                    errors.Add($"Rule type '{Type}' requires at least one enabled child rule");
                }

                foreach (var childRule in enabledRules)
                {
                    var childValidation = childRule.Validate();
                    errors.AddRange(childValidation.Errors.Select(error => $"AnyOf child: {error}"));
                }
            }
        }

        return new RuleValidationResult(errors.Count == 0, errors);
    }
}

/// <summary>
///     Result of validating a rule definition.
/// </summary>
/// <param name="IsValid">Whether the rule is valid.</param>
/// <param name="Errors">Any validation errors.</param>
public sealed record RuleValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    /// <summary>
    ///     A valid result with no errors.
    /// </summary>
    public static RuleValidationResult Valid => new(true, []);

    /// <summary>
    ///     Creates an invalid result with the specified errors.
    /// </summary>
    public static RuleValidationResult Invalid(params string[] errors) => new(false, errors);
}

/// <summary>
///     A policy ruleset stored in the database.
///     Contains an ordered list of rules to evaluate.
/// </summary>
public sealed class PolicyRuleset
{
    /// <summary>
    ///     The policy name (e.g., "Users.Edit", "Doc.Read").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Optional description for documentation/UI purposes.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     Whether this policy requires authentication.
    ///     Evaluated before any rules.
    /// </summary>
    public bool RequireAuthentication { get; init; } = true;

    /// <summary>
    ///     Ordered list of rules to evaluate.
    ///     All rules must pass (AND logic).
    ///     Use AnyOf rule for OR logic within a rule.
    /// </summary>
    public IReadOnlyList<RuleDefinition> Rules { get; init; } = [];

    /// <summary>
    ///     Version for cache invalidation.
    /// </summary>
    public long Version { get; init; } = 1;

    /// <summary>
    ///     Whether this ruleset is active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
