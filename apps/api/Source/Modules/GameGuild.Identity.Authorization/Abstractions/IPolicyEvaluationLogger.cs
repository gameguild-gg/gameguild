using System.Security.Claims;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Service interface for logging policy evaluation details.
///     Provides comprehensive debugging capabilities for authorization decisions.
/// </summary>
public interface IPolicyEvaluationLogger
{
    /// <summary>
    ///     Starts a new policy evaluation trace session.
    /// </summary>
    /// <param name="policyName">Name of the policy being evaluated.</param>
    /// <param name="user">The claims principal being evaluated.</param>
    /// <param name="resource">Optional resource being accessed.</param>
    /// <param name="correlationId">Optional correlation ID for request tracing.</param>
    /// <returns>A trace session that should be disposed when evaluation completes.</returns>
    IPolicyEvaluationTrace BeginTrace(
        string policyName,
        ClaimsPrincipal user,
        object? resource = null,
        string? correlationId = null);

    /// <summary>
    ///     Logs a requirement evaluation result.
    /// </summary>
    /// <param name="traceId">The trace session ID.</param>
    /// <param name="requirementName">Name of the requirement.</param>
    /// <param name="succeeded">Whether the requirement was satisfied.</param>
    /// <param name="reason">Optional reason for the result.</param>
    /// <param name="duration">Optional duration of the evaluation.</param>
    void LogRequirementResult(
        string traceId,
        string requirementName,
        bool succeeded,
        string? reason = null,
        TimeSpan? duration = null);

    /// <summary>
    ///     Logs policy evaluation completion.
    /// </summary>
    /// <param name="traceId">The trace session ID.</param>
    /// <param name="succeeded">Whether the policy was satisfied.</param>
    /// <param name="totalDuration">Total duration of the evaluation.</param>
    void LogPolicyResult(
        string traceId,
        bool succeeded,
        TimeSpan totalDuration);

    /// <summary>
    ///     Logs a policy evaluation failure with detailed context.
    /// </summary>
    /// <param name="traceId">The trace session ID.</param>
    /// <param name="failureReason">The reason for the failure.</param>
    /// <param name="suggestions">Optional suggestions for resolving the failure.</param>
    void LogPolicyFailure(
        string traceId,
        string failureReason,
        IEnumerable<string>? suggestions = null);

    /// <summary>
    ///     Gets the current debug settings for an endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint metadata.</param>
    /// <returns>Debug settings if enabled, null otherwise.</returns>
    PolicyDebugSettings? GetDebugSettings(object? endpoint);

    /// <summary>
    ///     Determines if debugging is enabled for the current context.
    /// </summary>
    /// <param name="endpoint">The endpoint metadata.</param>
    /// <returns>True if debugging should be performed.</returns>
    bool IsDebugEnabled(object? endpoint);
}

/// <summary>
///     Represents an active policy evaluation trace session.
/// </summary>
public interface IPolicyEvaluationTrace : IDisposable
{
    /// <summary>
    ///     Gets the unique trace ID for this evaluation session.
    /// </summary>
    string TraceId { get; }

    /// <summary>
    ///     Gets the policy name being evaluated.
    /// </summary>
    string PolicyName { get; }

    /// <summary>
    ///     Gets the start time of the evaluation.
    /// </summary>
    DateTime StartTime { get; }

    /// <summary>
    ///     Logs a requirement evaluation.
    /// </summary>
    /// <param name="requirementName">Name of the requirement.</param>
    /// <param name="succeeded">Whether it succeeded.</param>
    /// <param name="reason">Optional reason.</param>
    void LogRequirement(string requirementName, bool succeeded, string? reason = null);

    /// <summary>
    ///     Marks the trace as complete with a result.
    /// </summary>
    /// <param name="succeeded">Whether the policy evaluation succeeded.</param>
    void Complete(bool succeeded);

    /// <summary>
    ///     Adds context data to the trace.
    /// </summary>
    /// <param name="key">Context key.</param>
    /// <param name="value">Context value.</param>
    void AddContext(string key, object? value);
}

/// <summary>
///     Settings for policy debugging on an endpoint.
/// </summary>
public class PolicyDebugSettings
{
    /// <summary>
    ///     Gets or sets the log level for debugging.
    /// </summary>
    public PolicyDebugLogLevel LogLevel { get; set; } = PolicyDebugLogLevel.Standard;

    /// <summary>
    ///     Gets or sets whether to include stack traces.
    /// </summary>
    public bool IncludeStackTrace { get; set; }

    /// <summary>
    ///     Gets or sets whether to include user claims.
    /// </summary>
    public bool IncludeClaims { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether to include resource context.
    /// </summary>
    public bool IncludeResourceContext { get; set; } = true;

    /// <summary>
    ///     Gets or sets specific policy names to debug.
    /// </summary>
    public HashSet<string>? PolicyNames { get; set; }

    /// <summary>
    ///     Gets or sets the correlation header name.
    /// </summary>
    public string CorrelationHeader { get; set; } = "X-Policy-Debug-Id";
}
