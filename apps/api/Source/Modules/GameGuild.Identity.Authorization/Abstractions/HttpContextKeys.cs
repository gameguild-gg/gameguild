namespace GameGuild.Identity.Authorization;

/// <summary>
///     Constants for HttpContext.Items keys used throughout the authorization system.
///     Use these constants instead of magic strings to prevent typos causing runtime errors.
/// </summary>
/// <remarks>
///     <para>
///         HttpContext.Items is a per-request dictionary for storing request-scoped data.
///         Using string literals as keys is error-prone; these constants ensure consistency.
///     </para>
///     <para>
///         Example usage:
///         <code>
///         // Instead of: httpContext.Items["ActorContext"]
///         httpContext.Items[HttpContextKeys.ActorContext]
///         </code>
///     </para>
/// </remarks>
public static class HttpContextKeys
{
    // ========================
    // SECURITY CONTEXTS
    // ========================

    /// <summary>
    ///     Key for storing the current ActorContext in HttpContext.Items.
    ///     ActorContext is the primary security context for the request.
    /// </summary>
    public const string ActorContext = "ActorContext";

    /// <summary>
    ///     Key for storing the authorization tenant ID.
    ///     Used by HttpAuthorizationTenantContext.
    /// </summary>
    public const string AuthorizationTenantId = "AuthorizationTenantId";

    /// <summary>
    ///     Key for storing the active tenant membership role validated for this request.
    /// </summary>
    public const string AuthorizationTenantRole = "AuthorizationTenantRole";

    // ========================
    // LEGACY CONTEXTS (Deprecated)
    // ========================

    /// <summary>
    ///     Key for storing the legacy UserContext.
    /// </summary>
    /// <remarks>⚠️ Deprecated: Use ActorContext instead.</remarks>
    [Obsolete("Use HttpContextKeys.ActorContext instead. UserContext is deprecated.")]
    public const string UserContext = "UserContext";

    /// <summary>
    ///     Key for storing the legacy TenantContext.
    /// </summary>
    /// <remarks>⚠️ Deprecated: Use ActorContext.TenantId instead.</remarks>
    [Obsolete("Use HttpContextKeys.ActorContext and ActorContext.TenantId instead. TenantContext is deprecated.")]
    public const string TenantContext = "TenantContext";

    /// <summary>
    ///     Key for storing the legacy PermissionsContext.
    /// </summary>
    /// <remarks>⚠️ Deprecated: Use ActorContext instead.</remarks>
    [Obsolete("Use HttpContextKeys.ActorContext instead. PermissionsContext is deprecated.")]
    public const string PermissionsContext = "PermissionsContext";

    // ========================
    // LOCALIZATION
    // ========================

    /// <summary>
    ///     Key for storing the LocalizationContext.
    /// </summary>
    public const string LocalizationContext = "LocalizationContext";

    // ========================
    // REQUEST METADATA
    // ========================

    /// <summary>
    ///     Key for storing the correlation ID for request tracing.
    /// </summary>
    public const string CorrelationId = "CorrelationId";

    /// <summary>
    ///     Key for storing request timing information.
    /// </summary>
    public const string RequestStartTime = "RequestStartTime";

    /// <summary>
    ///     Key for storing the current tenant (object reference).
    /// </summary>
    public const string CurrentTenant = "CurrentTenant";

    // ========================
    // VALIDATION
    // ========================

    /// <summary>
    ///     All registered HttpContext keys for validation.
    /// </summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        ActorContext,
        AuthorizationTenantId,
        AuthorizationTenantRole,
#pragma warning disable CS0618 // Obsolete members included for validation
        UserContext,
        TenantContext,
        PermissionsContext,
#pragma warning restore CS0618
        LocalizationContext,
        CorrelationId,
        RequestStartTime,
        CurrentTenant
    };

    /// <summary>
    ///     Validates if a key is a known HttpContext key.
    /// </summary>
    /// <param name="key">The key to validate.</param>
    /// <returns>True if the key is known and registered.</returns>
    public static bool IsValid(string key) =>
        All.Contains(key, StringComparer.Ordinal);
}
