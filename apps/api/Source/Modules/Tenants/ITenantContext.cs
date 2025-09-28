namespace GameGuild.Core.Domain.Identity;

/// <summary> Interface for accessing current tenant context Domain interface for multi-tenancy concerns </summary>
public interface ITenantContext {
    /// <summary> Gets the current tenant ID </summary>
    Guid? TenantId { get; }

    /// <summary> Gets the current tenant name </summary>
    string? TenantName { get; }

    /// <summary> Gets tenant-specific settings </summary>
    IDictionary<string, object> Settings { get; }

    /// <summary> Checks if tenant is active </summary>
    bool IsActive { get; }

    /// <summary> Gets tenant subscription plan </summary>
    string? SubscriptionPlan { get; }
}