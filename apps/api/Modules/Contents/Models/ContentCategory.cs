namespace GameGuild.Modules.Contents.Models;

/// <summary>
/// Represents a category for organizing content
/// </summary>
[Table("ContentCategories")]
[Index(nameof(TenantId))]
[Index(nameof(ParentCategoryId))]
[Index(nameof(Slug), IsUnique = true)]
public class ContentCategory
{
    /// <summary>
    /// Unique identifier for the category
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Tenant identifier for multi-tenancy
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Category description
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Parent category for hierarchical structure
    /// </summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    [Required]
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether the category is active
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Icon or image URL
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// Color for UI display (hex code)
    /// </summary>
    [MaxLength(7)]
    public string? Color { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    /// <summary>
    /// When the category was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the category was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When the category was soft deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual ContentCategory? ParentCategory { get; set; }
    public virtual ICollection<ContentCategory> SubCategories { get; set; } = new List<ContentCategory>();
}
