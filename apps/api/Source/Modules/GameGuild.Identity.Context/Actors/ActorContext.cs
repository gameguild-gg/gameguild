namespace GameGuild.Identity.Context.Actors;

/// <summary>
///     Immutable, request-scoped security context representing the evaluated identity
///     and permissions for the current actor. This is the primary abstraction consumed
///     by authorization logic.
/// </summary>
/// <remarks>
///     <para>
///         ActorContext is placed in GameGuild.Identity.Context.Actors to keep it close to
///         the Actor abstractions it contains and to ensure the core abstraction module has
///         minimal ASP.NET dependencies.
///     </para>
///     <para>
///         This class is immutable (all properties are init-only) to ensure thread-safety
///         and prevent tampering during request processing.
///     </para>
/// </remarks>
public sealed record ActorContext
{
    private static readonly IReadOnlySet<string> EmptyStringSet = new HashSet<string>();

    /// <summary>
    ///     An empty/anonymous actor context for unauthenticated requests.
    /// </summary>
    public static readonly ActorContext Anonymous = new()
    {
        ActorKind = ActorKind.Anonymous,
        SubjectId = null,
        TenantId = null,
        Roles = EmptyStringSet,
        Permissions = EmptyStringSet,
        TypedAttributes = ActorAttributes.Empty,
        AuthScheme = null,
        IsAuthenticated = false
    };

    /// <summary>
    ///     Gets the kind of actor (User, Service, System, etc.).
    /// </summary>
    public required ActorKind ActorKind { get; init; }

    /// <summary>
    ///     Gets the unique subject identifier for the authenticated actor.
    ///     Null for anonymous/unauthenticated actors.
    /// </summary>
    /// <remarks>
    ///     For User actors, this is typically the user's GUID as a string.
    ///     For Service actors, this is the client/service ID.
    ///     For System actors, this is "system".
    /// </remarks>
    public string? SubjectId { get; init; }

    /// <summary>
    ///     Gets the current tenant ID, or null if not in a tenant context.
    /// </summary>
    /// <remarks>
    ///     The same subject can have different roles and permissions in different tenants.
    ///     Tenant resolution happens during request processing based on headers, claims,
    ///     or route data.
    /// </remarks>
    public Guid? TenantId { get; init; }

    /// <summary>
    ///     Gets the set of roles assigned to the actor in the current tenant context.
    /// </summary>
    /// <remarks>
    ///     Roles are tenant-scoped: the same user may have "Admin" role in one tenant
    ///     but only "Member" role in another.
    /// </remarks>
    public required IReadOnlySet<string> Roles { get; init; }

    /// <summary>
    ///     Gets the effective set of permissions for the actor in the current tenant context.
    /// </summary>
    /// <remarks>
    ///     Permissions are the union of:
    ///     - Permissions granted directly to the user
    ///     - Permissions inherited from the user's roles
    ///     - Permissions from any temporary elevation (JIT)
    ///     - Default tenant/global permissions
    /// </remarks>
    public required IReadOnlySet<string> Permissions { get; init; }

    /// <summary>
    ///     Gets additional attributes/claims about the actor.
    /// </summary>
    /// <remarks>
    ///     Useful for ABAC (Attribute-Based Access Control) scenarios.
    ///     Common attributes: email, email_verified, mfa_verified, department, etc.
    /// </remarks>
    [Obsolete("Use TypedAttributes for strongly-typed access. This property is maintained for backward compatibility.")]
    public IReadOnlyDictionary<string, string> Attributes => TypedAttributes.ToDictionary();

    /// <summary>
    ///     Gets strongly-typed attributes/claims about the actor.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Provides compile-time safe access to common actor attributes for ABAC scenarios.
    ///         Replaces the stringly-typed <see cref="Attributes"/> dictionary.
    ///     </para>
    ///     <para>
    ///         Common attributes: Email, EmailVerified, MfaVerified, Department, TenantRole, etc.
    ///         For custom/domain-specific attributes, use <see cref="ActorAttributes.Custom"/>.
    ///     </para>
    /// </remarks>
    public ActorAttributes TypedAttributes { get; init; } = ActorAttributes.Empty;

    /// <summary>
    ///     Gets the authentication scheme used (e.g., "Bearer", "ApiKey", "mTLS").
    /// </summary>
    public string? AuthScheme { get; init; }

    /// <summary>
    ///     Gets whether the actor is authenticated.
    /// </summary>
    public required bool IsAuthenticated { get; init; }

    /// <summary>
    ///     Gets whether the actor is a system administrator with full access.
    /// </summary>
    public bool IsSystemAdmin => Roles.Contains("SystemAdmin") || Roles.Contains("Admin");

    /// <summary>
    ///     Gets whether the actor is a tenant administrator for the current tenant.
    /// </summary>
    public bool IsTenantAdmin => Roles.Contains("TenantAdmin") || IsSystemAdmin;

    /// <summary>
    ///     Gets the subject ID as a GUID, or null if not a valid GUID.
    /// </summary>
    public Guid? SubjectIdAsGuid => Guid.TryParse(SubjectId, out var guid) ? guid : null;

    /// <summary>
    ///     Checks if the actor has a specific permission (string-based, legacy).
    /// </summary>
    /// <param name="permission">The permission key to check (e.g., "users:read").</param>
    /// <returns>True if the actor has the permission.</returns>
    /// <remarks>
    ///     Consider using the strongly-typed overload: HasPermission(Permission permission)
    ///     for compile-time safety and prevention of typo-based security bypasses.
    /// </remarks>
    public bool HasPermission(string permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        // System admins have all permissions
        if (IsSystemAdmin)
            return true;

        // Check for wildcard admin permission
        if (Permissions.Contains("admin:*"))
            return true;

        return Permissions.Contains(permission);
    }

    /// <summary>
    ///     Checks if the actor has a specific permission (strongly-typed).
    /// </summary>
    /// <param name="permission">The strongly-typed permission to check.</param>
    /// <returns>True if the actor has the permission.</returns>
    /// <remarks>
    ///     This overload provides compile-time safety and prevents typo-based security bypasses.
    ///     Example: actor.HasPermission(UsersPermission.Read) instead of actor.HasPermission("users:read")
    /// </remarks>
    public bool HasPermission(object permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        // Handle Permission objects via implicit string conversion
        var permissionKey = permission.ToString();
        return HasPermission(permissionKey!);
    }

    /// <summary>
    ///     Checks if the actor has any of the specified permissions (string-based, legacy).
    /// </summary>
    /// <param name="permissions">The permission keys to check.</param>
    /// <returns>True if the actor has at least one of the permissions.</returns>
    /// <remarks>
    ///     Consider using the strongly-typed overload for compile-time safety.
    /// </remarks>
    public bool HasAnyPermission(params string[] permissions)
    {
        if (permissions.Length == 0)
            return false;

        if (IsSystemAdmin)
            return true;

        return permissions.Any(HasPermission);
    }

    /// <summary>
    ///     Checks if the actor has any of the specified permissions (strongly-typed).
    /// </summary>
    /// <param name="permissions">The strongly-typed permissions to check.</param>
    /// <returns>True if the actor has at least one of the permissions.</returns>
    /// <remarks>
    ///     This overload provides compile-time safety.
    ///     Example: actor.HasAnyPermission(UsersPermission.Read, UsersPermission.ReadSelf)
    /// </remarks>
    public bool HasAnyPermission(params object[] permissions)
    {
        if (permissions.Length == 0)
            return false;

        if (IsSystemAdmin)
            return true;

        return permissions.Any(p => HasPermission(p));
    }

    /// <summary>
    ///     Checks if the actor has all of the specified permissions (string-based, legacy).
    /// </summary>
    /// <param name="permissions">The permission keys to check.</param>
    /// <returns>True if the actor has all of the permissions.</returns>
    /// <remarks>
    ///     Consider using the strongly-typed overload for compile-time safety.
    /// </remarks>
    public bool HasAllPermissions(params string[] permissions)
    {
        if (permissions.Length == 0)
            return true;

        if (IsSystemAdmin)
            return true;

        return permissions.All(HasPermission);
    }

    /// <summary>
    ///     Checks if the actor has all of the specified permissions (strongly-typed).
    /// </summary>
    /// <param name="permissions">The strongly-typed permissions to check.</param>
    /// <returns>True if the actor has all of the permissions.</returns>
    /// <remarks>
    ///     This overload provides compile-time safety.
    ///     Example: actor.HasAllPermissions(UsersPermission.Read, UsersPermission.Write)
    /// </remarks>
    public bool HasAllPermissions(params object[] permissions)
    {
        if (permissions.Length == 0)
            return true;

        if (IsSystemAdmin)
            return true;

        return permissions.All(p => HasPermission(p));
    }

    /// <summary>
    ///     Checks if the actor is in the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the actor is in the role.</returns>
    public bool IsInRole(string role)
    {
        ArgumentNullException.ThrowIfNull(role);
        return Roles.Contains(role);
    }

    /// <summary>
    ///     Gets an attribute value by key.
    ///     Checks both typed properties (e.g. "email" → Email) and custom attributes.
    /// </summary>
    /// <param name="key">The attribute key.</param>
    /// <returns>The attribute value, or null if not found.</returns>
    public string? GetAttribute(string key)
    {
        // Check custom attributes first (fast path for unknown keys)
        var customValue = TypedAttributes.GetCustomAttribute(key);
        if (customValue != null) return customValue;

        // Fall back to typed attributes (handles known keys like "email", "name", etc.)
        var allAttributes = TypedAttributes.ToDictionary();
        return allAttributes.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    ///     Checks if the actor has MFA verified in the current session.
    /// </summary>
    public bool IsMfaVerified => TypedAttributes.MfaVerified;
}
