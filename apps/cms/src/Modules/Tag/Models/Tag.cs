using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;

namespace GameGuild.Modules.Tag.Models;

[Table("tags")]
[Index(nameof(Name))]
[Index(nameof(Type))]
[Index(nameof(IsActive))]
[Index(nameof(TenantId))]
[Index(nameof(Name), nameof(TenantId), IsUnique = true)]
public class Tag : BaseEntity, ITenantable
{
    private string _name = string.Empty;

    private string? _description;

    private TagType _type;

    private string? _color;

    private string? _icon;

    private bool _isActive = true;

    private Guid? _tenantId;

    private ICollection<TagRelationship> _sourceRelationships = new List<TagRelationship>();

    private ICollection<TagRelationship> _targetRelationships = new List<TagRelationship>();

    [Required]
    [MaxLength(100)]
    public string Name
    {
        get => _name;
        set => _name = value;
    }

    [MaxLength(500)]
    public string? Description
    {
        get => _description;
        set => _description = value;
    }

    public TagType Type
    {
        get => _type;
        set => _type = value;
    }

    /// <summary>
    /// Hexadecimal color code for UI display
    /// </summary>
    [MaxLength(7)]
    public string? Color
    {
        get => _color;
        set => _color = value;
    }

    /// <summary>
    /// Icon identifier for UI display
    /// </summary>
    [MaxLength(100)]
    public string? Icon
    {
        get => _icon;
        set => _icon = value;
    }

    /// <summary>
    /// Whether this tag is available for use
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => _isActive = value;
    }

    public Guid? TenantId
    {
        get => _tenantId;
        set => _tenantId = value;
    }

    // Navigation properties
    public virtual ICollection<TagRelationship> SourceRelationships
    {
        get => _sourceRelationships;
        set => _sourceRelationships = value;
    }

    public virtual ICollection<TagRelationship> TargetRelationships
    {
        get => _targetRelationships;
        set => _targetRelationships = value;
    }
}
