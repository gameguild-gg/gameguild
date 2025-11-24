using GameGuild.Abstractions;

namespace GameGuild.Tenants.Abstractions;

/// <summary>
///     Interface that defines the contract for tenant entities in a multi-tenant system.
///     Extends IEntity to provide tenant-specific properties and operations.
/// </summary>
public interface ITenant : IEntity
{
    /// <summary>
    ///     Name of the tenant
    /// </summary>
    string Name { get; set; }

    /// <summary>
    ///     Description of the tenant
    /// </summary>
    string? Description { get; set; }

    /// <summary>
    ///     Whether this tenant is currently active
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    ///     Slug for the tenant (URL-friendly unique identifier)
    /// </summary>
    string Slug { get; set; }

    /// <summary>
    ///     Administrative email for the tenant
    /// </summary>
    string? AdminEmail { get; set; }

    /// <summary>
    ///     Activate the tenant
    /// </summary>
    void Activate();

    /// <summary>
    ///     Deactivate the tenant
    /// </summary>
    void Deactivate();

    /// <summary>
    ///     Update tenant information
    /// </summary>
    void Update(string name, string? description = null);
}
