namespace GameGuild.Modules.Tenants;

/// <summary>
/// Data Transfer Object for Tenant information
/// </summary>
public record TenantDto
{
    /// <summary>
    /// Unique identifier for the tenant
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Name of the tenant
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of the tenant
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether this tenant is currently active
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Slug for the tenant (URL-friendly unique identifier)
    /// </summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>
    /// The date and time when the tenant was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// The date and time when the tenant was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Whether the tenant has been deleted (soft-deleted)
    /// </summary>
    public bool IsDeleted { get; init; }
}
