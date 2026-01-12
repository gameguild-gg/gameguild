using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Represents a tenant in a multi-tenant system.
///     This is the aggregate root for the Tenant bounded context, managing all tenant-related entities.
///     Inherits from EntityBase to provide UUID IDs, version control, timestamps, and soft delete functionality.
/// </summary>
/// <remarks>
///     <para>
///         <b>Aggregate Root Pattern:</b> The Tenant entity is the aggregate root for the following child entities:
///         <list type="bullet">
///             <item><see cref="TenantMembers"/> - User memberships within this tenant</item>
///             <item><see cref="TenantDomains"/> - Custom domains configured for this tenant</item>
///             <item><see cref="TenantSettings"/> - Tenant-specific configuration (1:1 relationship)</item>
///             <item><see cref="TenantStatistics"/> - Analytics and metrics for this tenant (1:1 relationship)</item>
///             <item><see cref="UsageTrackingRecords"/> - Resource consumption tracking</item>
///         </list>
///         This follows DDD aggregate root principles where the Tenant is the consistency boundary for these related entities.
///     </para>
///     <para>
///         <b>Design Rationale (NOT a God Object):</b> While this entity has multiple navigation properties, it adheres to
///         the Single Responsibility Principle because:
///         <list type="number">
///             <item>Each child entity (Settings, Statistics, Usage) is a distinct bounded context that belongs to the Tenant aggregate</item>
///             <item>The Tenant entity itself only contains core identity properties (Name, Slug, Status) and lifecycle methods</item>
///             <item>Child entities encapsulate their own behavior and are accessed through the aggregate root</item>
///             <item>All child entities share the same TenantId, making Tenant the natural consistency boundary</item>
///         </list>
///         External modules should access child entities through repositories using TenantId, not by traversing through the Tenant entity.
///     </para>
///     <para>
///         <b>Cross-Module Relationship:</b> The <see cref="TenantMembers"/> collection links to <c>User</c> entities from
///         the <c>GameGuild.Identity.Users</c> module. The <c>User</c> entity has a corresponding <c>TenantMemberships</c>
///         navigation property for bidirectional traversal, enabling efficient queries in both directions.
///     </para>
///     <para>
///         <b>Domain Events:</b> Lifecycle methods (<see cref="Activate"/>, <see cref="Deactivate"/>, <see cref="Update"/>,
///         <see cref="Archive"/>, <see cref="Unarchive"/>) emit domain events for integration with other modules and audit logging.
///     </para>
/// </remarks>
[Table("Tenants")]
[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Slug), IsUnique = true)]
public class Tenant : EntityBase, ITenant
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public Tenant() { }

    /// <summary>
    ///     Constructor for partial initialization
    /// </summary>
    /// <param name="partial">Partial tenant data</param>
    public Tenant(object partial) : base(partial) { }

    /// <summary>
    ///     Whether this is the default tenant (for null tenant scenarios)
    ///     PostgreSQL filtered unique index ensures only one tenant can be default
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    ///     Whether this tenant is archived (distinct from deleted)
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    ///     When the tenant was archived (null if not archived)
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>
    ///     Navigation property to tenant members
    /// </summary>
    public virtual ICollection<TenantMember> TenantMembers { get; } = new List<TenantMember>();

    /// <summary>
    ///     Navigation property to tenant domains
    /// </summary>
    public virtual ICollection<TenantDomain> TenantDomains { get; } = new List<TenantDomain>();

    /// <summary>
    ///     Navigation property to tenant settings
    /// </summary>
    public virtual TenantSettings? TenantSettings { get; set; }

    /// <summary>
    ///     Navigation property to tenant statistics
    /// </summary>
    public virtual TenantStatistics? TenantStatistics { get; set; }

    /// <summary>
    ///     Navigation property to usage tracking records
    /// </summary>
    public virtual ICollection<UsageTracking> UsageTrackingRecords { get; } = new List<UsageTracking>();

    /// <summary>
    ///     Name of the tenant
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Description of the tenant
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    ///     Whether this tenant is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Slug for the tenant (URL-friendly unique identifier)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    ///     Administrative email for the tenant
    /// </summary>
    [MaxLength(255)]
    public string? AdminEmail { get; set; }

    /// <summary>
    ///     Activate the tenant
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();

        // Add domain event
        AddDomainEvent(new TenantActivatedEvent(Id, Name));
    }

    /// <summary>
    ///     Deactivate the tenant
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        Touch();

        // Add domain event
        AddDomainEvent(new TenantDeactivatedEvent(Id, Name));
    }

    /// <summary>
    ///     Update tenant information
    /// </summary>
    public void Update(string name, string? description = null)
    {
        Name = name;
        Description = description;
        Touch();

        // Add domain event
        AddDomainEvent(new TenantUpdatedEvent(Id, name, description));
    }

    /// <summary>
    ///     Archive the tenant
    /// </summary>
    public void Archive(string reason = "")
    {
        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
        IsActive = false; // Archived tenants are also inactive
        Touch();

        // Add domain event
        AddDomainEvent(new TenantArchivedEvent(Id, reason));
    }

    /// <summary>
    ///     Unarchive/restore the tenant
    /// </summary>
    public void Unarchive()
    {
        IsArchived = false;
        ArchivedAt = null;
        IsActive = true; // Unarchived tenants become active again
        Touch();

        // Add domain event
        AddDomainEvent(new TenantRestoredEvent(Id, Name));
    }
}
