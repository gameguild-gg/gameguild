namespace GameGuild.Core.Domain.Identity;

/// <summary>
/// Interface for accessing current user context
/// Domain interface for user identity concerns
/// </summary>
public interface IUserContext {
    /// <summary>
    /// Gets the current user ID
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user email
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the current user name
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets all user claims
    /// </summary>
    IDictionary<string, object> Claims { get; }

    /// <summary>
    /// Checks if user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Checks if user has specific role
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Gets user roles
    /// </summary>
    IEnumerable<string> Roles { get; }
}

/// <summary>
/// Interface for accessing current tenant context
/// Domain interface for multi-tenancy concerns
/// </summary>
public interface ITenantContext {
    /// <summary>
    /// Gets the current tenant ID
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Gets the current tenant name
    /// </summary>
    string? TenantName { get; }

    /// <summary>
    /// Gets tenant-specific settings
    /// </summary>
    IDictionary<string, object> Settings { get; }

    /// <summary>
    /// Checks if tenant is active
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets tenant subscription plan
    /// </summary>
    string? SubscriptionPlan { get; }
}

/// <summary>
/// Context interface for permissions checking within a request scope
/// </summary>
public interface IPermissionsContext {
    /// <summary>
    /// Gets the current user ID from the context
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current tenant ID from the context
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Indicates if the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Indicates if the current user is a system administrator
    /// </summary>
    bool IsSystemAdmin { get; }

    /// <summary>
    /// Indicates if the current user is a tenant administrator
    /// </summary>
    bool IsTenantAdmin { get; }

    /// <summary>
    /// Checks if the current user has a specific tenant permission
    /// </summary>
    /// <param name="permission">The permission type to check</param>
    /// <param name="tenantId">Optional specific tenant ID (defaults to current tenant)</param>
    /// <returns>True if the user has the permission</returns>
    Task<bool> HasTenantPermissionAsync(PermissionType permission, Guid? tenantId = null);

    /// <summary>
    /// Checks if the current user has a specific content type permission
    /// </summary>
    /// <param name="permission">The permission type to check</param>
    /// <param name="contentType">The content type to check</param>
    /// <param name="tenantId">Optional specific tenant ID</param>
    /// <returns>True if the user has the permission</returns>
    Task<bool> HasContentTypePermissionAsync(PermissionType permission, string contentType, Guid? tenantId = null);

    /// <summary>
    /// Checks if the current user has a specific resource permission
    /// </summary>
    /// <param name="permission">The permission type to check</param>
    /// <param name="resourceType">The resource type</param>
    /// <param name="resourceId">The resource ID</param>
    /// <param name="tenantId">Optional specific tenant ID</param>
    /// <returns>True if the user has the permission</returns>
    Task<bool> HasResourcePermissionAsync(PermissionType permission, string resourceType, Guid resourceId, Guid? tenantId = null);

    /// <summary>
    /// Checks if the current user has any of the specified permissions
    /// </summary>
    /// <param name="permissions">Array of permissions to check</param>
    /// <param name="tenantId">Optional specific tenant ID</param>
    /// <returns>True if the user has any of the permissions</returns>
    Task<bool> HasAnyTenantPermissionAsync(PermissionType[] permissions, Guid? tenantId = null);
}

/// <summary>
/// Context interface for resource identification within a request scope
/// </summary>
public interface IResourceContext {
    /// <summary>
    /// Gets the current resource ID if available
    /// </summary>
    Guid? ResourceId { get; }

    /// <summary>
    /// Gets the current resource type if available
    /// </summary>
    string? ResourceType { get; }

    /// <summary>
    /// Gets a string identifier for the current resource
    /// </summary>
    /// <returns>Resource identifier string</returns>
    string GetResourceIdentifier();

    /// <summary>
    /// Sets the current resource context
    /// </summary>
    /// <param name="resourceType">The resource type</param>
    /// <param name="resourceId">The resource ID</param>
    void SetResource(string resourceType, Guid resourceId);

    /// <summary>
    /// Clears the current resource context
    /// </summary>
    void ClearResource();
}

/// <summary>
/// Context interface for localization within a request scope
/// </summary>
public interface ILocalizationContext {
    /// <summary>
    /// Gets the current culture
    /// </summary>
    System.Globalization.CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Gets the current UI culture
    /// </summary>
    System.Globalization.CultureInfo CurrentUICulture { get; }

    /// <summary>
    /// Gets the current time zone
    /// </summary>
    TimeZoneInfo CurrentTimeZone { get; }

    /// <summary>
    /// Gets the current time zone ID
    /// </summary>
    string TimeZoneId { get; }

    /// <summary>
    /// Gets the current local time in the user's time zone
    /// </summary>
    /// <returns>Current local time</returns>
    DateTime GetCurrentLocalTime();

    /// <summary>
    /// Converts UTC time to local time using the current time zone
    /// </summary>
    /// <param name="utcTime">UTC time to convert</param>
    /// <param name="localTime">Local time</param>
    /// <returns>Local time</returns>
    DateTime ConvertToLocalTime(DateTime utcTime);

    /// <summary>
    /// Converts local time to UTC using the current time zone
    /// </summary>
    /// <param name="localTime">Local time to convert</param>
    /// <returns>UTC time</returns>
    DateTime ConvertToUtcTime(DateTime localTime);
}
