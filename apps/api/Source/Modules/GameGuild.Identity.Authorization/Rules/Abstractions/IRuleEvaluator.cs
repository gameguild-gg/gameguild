using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Represents an authorization rule that can be evaluated as part of a policy.
///     Rules are "code-defined, data-configured" building blocks for policies.
/// </summary>
public interface IRuleEvaluator
{
    /// <summary>
    ///     Gets the unique type identifier for this rule.
    ///     Used to match rule definitions in the database to evaluator implementations.
    /// </summary>
    string RuleType { get; }

    /// <summary>
    ///     Evaluates the rule against the current authorization context.
    /// </summary>
    /// <param name="context">The authorization handler context</param>
    /// <param name="parameters">Rule parameters from database configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The evaluation result</returns>
    Task<RuleEvaluationResult> EvaluateAsync(
        AuthorizationHandlerContext context,
        RuleParameters parameters,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Result of a rule evaluation.
/// </summary>
public sealed record RuleEvaluationResult
{
    /// <summary>
    ///     Creates a successful result.
    /// </summary>
    public static RuleEvaluationResult Success() => new() { IsSuccess = true };

    /// <summary>
    ///     Creates a failed result with a reason.
    /// </summary>
    public static RuleEvaluationResult Fail(string reason) => new() { IsSuccess = false, FailureReason = reason };

    /// <summary>
    ///     Creates a skipped result (rule doesn't apply in this context).
    /// </summary>
    public static RuleEvaluationResult Skip(string reason) => new() { IsSuccess = true, IsSkipped = true, FailureReason = reason };

    /// <summary>
    ///     Whether the rule evaluation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    ///     Whether the rule was skipped (doesn't apply to this context).
    /// </summary>
    public bool IsSkipped { get; init; }

    /// <summary>
    ///     Reason for failure or skip.
    /// </summary>
    public string? FailureReason { get; init; }
}
