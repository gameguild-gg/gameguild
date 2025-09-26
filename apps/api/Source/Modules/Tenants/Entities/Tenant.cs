namespace GameGuild.Modules.Tenants;

/// <summary> Represents a tenant in a multi-tenant system Inherits from EntityBase to provide UUID IDs, version control, timestamps, and soft delete functionality A tenant is a standalone entity that doesn't belong to another tenant (no circular reference) </summary>
[Table("tenants")]
[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(IsActive))]
public class Tenant : EntityBase
{
    /// <summary> Default constructor </summary>
    public Tenant() { }

    /// <summary> Name of the tenant </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary> Description of the tenant </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary> Whether this tenant is currently active </summary>
    public bool IsActive { get; set; } = true;

    /// <summary> Whether this is the default tenant (for null tenant scenarios) PostgreSQL filtered unique index ensures only one tenant can be default </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary> Slug for the tenant (URL-friendly unique identifier) </summary>
    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;

    /// <summary> Activate the tenant </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    /// <summary> Deactivate the tenant </summary>
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    /// <summary> Update tenant information </summary>
    public void Update(string name, string? description = null)
    {
        Name = name;
        Description = description;
        Touch();
    }
}
