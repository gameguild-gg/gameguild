namespace GameGuild.Identity.Authorization;

/// <summary>
///     Exception thrown when permission fetching fails during ActorContext construction.
///     This triggers fail-closed error handling in ActorContextMiddleware.
/// </summary>
/// <remarks>
///     <para>
///         This exception is part of the fail-closed security design. When permissions cannot
///         be fetched from the database, the middleware catches this exception and:
///     </para>
///     <list type="number">
///         <item>Sets ActorContext to Anonymous (denies all permissions)</item>
///         <item>Logs the security event for audit</item>
///         <item>Returns HTTP 500 to prevent request processing</item>
///     </list>
///     <para>
///         This prevents potential privilege escalation from stale JWT token permissions
///         when the permission database is unavailable or experiencing errors.
///     </para>
/// </remarks>
public sealed class PermissionFetchException : Exception
{
    /// <summary>
    ///     Gets the subject ID (user ID) for which permission fetching failed.
    /// </summary>
    public Guid SubjectId { get; }

    /// <summary>
    ///     Gets the tenant ID for which permission fetching failed.
    /// </summary>
    public Guid TenantId { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PermissionFetchException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="subjectId">The subject ID (user ID).</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="innerException">The underlying exception that caused the failure.</param>
    public PermissionFetchException(
        string message, 
        Guid subjectId, 
        Guid tenantId, 
        Exception? innerException = null)
        : base(message, innerException)
    {
        SubjectId = subjectId;
        TenantId = tenantId;
    }
}
