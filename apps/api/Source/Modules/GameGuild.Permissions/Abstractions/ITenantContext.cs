namespace GameGuild.Permissions.Domain.Abstractions;

/// <summary>
///     Abstraction for accessing current tenant information from the request context
///     Provides a testable interface for tenant-related claims and properties
/// </summary>
public interface ITenantContext
{
    /// <summary>
    ///     Gets the current tenant ID
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    ///     Gets the current tenant name
    /// </summary>
    string? TenantName { get; }

    /// <summary>
    ///     Gets tenant-specific settings as a dictionary
    /// </summary>
    IDictionary<string, object> Settings { get; }

    /// <summary>
    ///     Gets whether the current tenant is active
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    ///     Gets the subscription plan for the current tenant
    /// </summary>
    string? SubscriptionPlan { get; }
}
