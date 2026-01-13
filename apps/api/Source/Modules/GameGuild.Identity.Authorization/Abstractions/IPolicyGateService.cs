namespace GameGuild.Identity.Authorization;

/// <summary>
///     Unified service for evaluating policy gates (deny-focused).
///     Gates are evaluated first and can deny access before permission resolution.
/// </summary>
/// <remarks>
///     <para>
///         <b>Evaluation Policy: DENY-WINS (First-Fail)</b>
///         Any gate failure immediately denies access. Gates are evaluated in order:
///         <list type="number">
///             <item>Conditional policies (time windows, IP ranges, expiration)</item>
///             <item>ABAC policies (attribute-based access control)</item>
///             <item>Environment policies (MFA required, device trust)</item>
///         </list>
///     </para>
///     <para>
///         <b>Static vs Dynamic Gates:</b>
///         <list type="bullet">
///             <item>Static gates are hard-coded rules that cannot be overridden (e.g., system maintenance mode)</item>
///             <item>Dynamic gates are loaded from database and can be modified at runtime</item>
///         </list>
///     </para>
/// </remarks>
public interface IPolicyGateService
{
    /// <summary>
    ///     Evaluates all policy gates for the given context.
    ///     Returns immediately on first gate failure.
    /// </summary>
    /// <param name="context">The gate evaluation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Gate evaluation result with details on any denial.</returns>
    Task<PolicyGateResult> EvaluateGatesAsync(
        PolicyGateContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Evaluates a specific gate type only.
    /// </summary>
    /// <param name="gateType">The type of gate to evaluate.</param>
    /// <param name="context">The gate evaluation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Gate evaluation result.</returns>
    Task<PolicyGateResult> EvaluateGateAsync(
        PolicyGateType gateType,
        PolicyGateContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Context for policy gate evaluation.
/// </summary>
public record PolicyGateContext
{
    /// <summary>
    ///     The actor ID (user/service) requesting access.
    /// </summary>
    public required Guid ActorId { get; init; }

    /// <summary>
    ///     The resource type being accessed.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    ///     The resource identifier (optional).
    /// </summary>
    public Guid? ResourceId { get; init; }

    /// <summary>
    ///     The action being performed.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    ///     The tenant context.
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    ///     Client IP address.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    ///     User agent string.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    ///     Device fingerprint (if available).
    /// </summary>
    public string? DeviceFingerprint { get; init; }

    /// <summary>
    ///     Geographic location (if available).
    /// </summary>
    public string? GeoLocation { get; init; }

    /// <summary>
    ///     Additional attributes for ABAC evaluation.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Attributes { get; init; }

    /// <summary>
    ///     Request timestamp.
    /// </summary>
    public DateTime RequestTime { get; init; } = DateTime.UtcNow;
}

/// <summary>
///     Result of policy gate evaluation.
/// </summary>
public record PolicyGateResult
{
    /// <summary>
    ///     Whether all gates passed (access allowed to proceed to permission check).
    /// </summary>
    public bool IsAllowed { get; init; }

    /// <summary>
    ///     The gate that denied access (if any).
    /// </summary>
    public PolicyGateType? DeniedByGate { get; init; }

    /// <summary>
    ///     The specific policy that denied access (if any).
    /// </summary>
    public string? DeniedByPolicyId { get; init; }

    /// <summary>
    ///     Human-readable reason for denial.
    /// </summary>
    public string? DenialReason { get; init; }

    /// <summary>
    ///     Details of each gate evaluation for debugging.
    /// </summary>
    public IReadOnlyList<GateEvaluationDetail>? GateDetails { get; init; }

    /// <summary>
    ///     Creates a successful result (all gates passed).
    /// </summary>
    public static PolicyGateResult Allowed(IReadOnlyList<GateEvaluationDetail>? details = null)
        => new() { IsAllowed = true, GateDetails = details };

    /// <summary>
    ///     Creates a denied result.
    /// </summary>
    public static PolicyGateResult Denied(
        PolicyGateType gate,
        string reason,
        string? policyId = null,
        IReadOnlyList<GateEvaluationDetail>? details = null)
        => new()
        {
            IsAllowed = false,
            DeniedByGate = gate,
            DeniedByPolicyId = policyId,
            DenialReason = reason,
            GateDetails = details
        };
}

/// <summary>
///     Detail of a single gate evaluation.
/// </summary>
public record GateEvaluationDetail(
    PolicyGateType GateType,
    bool Passed,
    string? PolicyId,
    string? Reason,
    TimeSpan EvaluationTime);

/// <summary>
///     Types of policy gates.
/// </summary>
public enum PolicyGateType
{
    /// <summary>
    ///     Conditional policies (time windows, IP ranges, expiration).
    /// </summary>
    Conditional = 1,

    /// <summary>
    ///     ABAC policies (attribute-based access control).
    /// </summary>
    Abac = 2,

    /// <summary>
    ///     Environment policies (MFA required, device trust).
    /// </summary>
    Environment = 3,

    /// <summary>
    ///     Static/hard-coded policies (system maintenance, emergency lockdown).
    /// </summary>
    Static = 4
}
