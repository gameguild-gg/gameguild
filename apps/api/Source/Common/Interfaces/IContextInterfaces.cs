namespace GameGuild.Common.Interfaces;

/// <summary>
/// Interface for accessing current user context
/// </summary>
public interface IUserContext
{
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
/// </summary>
public interface ITenantContext
{
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
