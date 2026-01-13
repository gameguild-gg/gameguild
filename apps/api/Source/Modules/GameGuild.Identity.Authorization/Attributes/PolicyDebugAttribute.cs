namespace GameGuild.Identity.Authorization;

/// <summary>
///     Enables verbose policy evaluation logging for debugging authorization issues.
///     Apply to controllers or action methods to capture detailed policy evaluation traces.
/// </summary>
/// <remarks>
///     When applied, the authorization pipeline will emit detailed logs including:
///     - Policy names being evaluated
///     - Requirement evaluation results (pass/fail)
///     - Resource context and claims being checked
///     - Timing information for performance analysis
///     - Failure reasons and suggestions
///     
///     WARNING: Do not use in production with sensitive data as logs may contain
///     user claims, resource IDs, and authorization context details.
/// </remarks>
/// <example>
///     <code>
///     [PolicyDebug]
///     [Authorize(Policy = "CanEditPosts")]
///     public async Task&lt;IActionResult&gt; EditPost(Guid id)
///     {
///         // Policy evaluation will be logged in detail
///     }
///     
///     [PolicyDebug(LogLevel = PolicyDebugLogLevel.Verbose, IncludeStackTrace = true)]
///     [Authorize]
///     public class AdminController : ControllerBase
///     {
///         // All actions will have verbose policy debugging
///     }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class PolicyDebugAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of <see cref="PolicyDebugAttribute"/>.
    /// </summary>
    public PolicyDebugAttribute()
    {
        LogLevel = PolicyDebugLogLevel.Standard;
        IncludeStackTrace = false;
        IncludeClaims = true;
        IncludeResourceContext = true;
        CorrelationHeader = "X-Policy-Debug-Id";
    }

    /// <summary>
    ///     Gets or sets the logging detail level.
    /// </summary>
    public PolicyDebugLogLevel LogLevel { get; set; }

    /// <summary>
    ///     Gets or sets whether to include stack traces in failure logs.
    ///     Default is false for performance reasons.
    /// </summary>
    public bool IncludeStackTrace { get; set; }

    /// <summary>
    ///     Gets or sets whether to include user claims in logs.
    ///     Default is true. Set to false when dealing with sensitive claim data.
    /// </summary>
    public bool IncludeClaims { get; set; }

    /// <summary>
    ///     Gets or sets whether to include resource context in logs.
    ///     Default is true. Set to false when resource data is sensitive.
    /// </summary>
    public bool IncludeResourceContext { get; set; }

    /// <summary>
    ///     Gets or sets the header name for correlation ID.
    ///     Default is "X-Policy-Debug-Id".
    /// </summary>
    public string CorrelationHeader { get; set; }

    /// <summary>
    ///     Gets or sets specific policy names to debug.
    ///     If null or empty, all policies are debugged.
    /// </summary>
    public string[]? PolicyNames { get; set; }

    /// <summary>
    ///     Gets or sets whether debugging is enabled.
    ///     Allows conditional enabling via configuration or feature flags.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
///     Defines the level of detail for policy debugging logs.
/// </summary>
public enum PolicyDebugLogLevel
{
    /// <summary>
    ///     Minimal logging - only policy names and final results.
    /// </summary>
    Minimal = 0,

    /// <summary>
    ///     Standard logging - includes requirement results and timing.
    /// </summary>
    Standard = 1,

    /// <summary>
    ///     Verbose logging - includes all context, claims, and detailed traces.
    /// </summary>
    Verbose = 2,

    /// <summary>
    ///     Diagnostic logging - maximum detail including internal state.
    ///     WARNING: May include sensitive information and impact performance.
    /// </summary>
    Diagnostic = 3
}
