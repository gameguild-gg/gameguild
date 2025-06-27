using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;
using GameGuild.Modules.Certificate.Models;

namespace GameGuild.Modules.Tag.Models;

[Table("tag_proficiencies")]
[Index(nameof(Name))]
[Index(nameof(Type))]
[Index(nameof(ProficiencyLevel))]
[Index(nameof(IsActive))]
public class TagProficiency : BaseEntity {
  private string _name = string.Empty;

  private string? _description;

  private TagType _type;

  private SkillProficiencyLevel _proficiencyLevel;

  private string? _color;

  private string? _icon;

  private bool _isActive = true;

  private ICollection<CertificateTag> _certificateTags = new List<Certificate.Models.CertificateTag>();

  [Required]
  [MaxLength(100)]
  public string Name {
    get => _name;
    set => _name = value;
  }

  [MaxLength(500)]
  public string? Description {
    get => _description;
    set => _description = value;
  }

  public TagType Type {
    get => _type;
    set => _type = value;
  }

  public SkillProficiencyLevel ProficiencyLevel {
    get => _proficiencyLevel;
    set => _proficiencyLevel = value;
  }

  /// <summary>
  /// Hexadecimal color code for UI display
  /// </summary>
  [MaxLength(7)]
  public string? Color {
    get => _color;
    set => _color = value;
  }

  /// <summary>
  /// Icon identifier for UI display
  /// </summary>
  [MaxLength(100)]
  public string? Icon {
    get => _icon;
    set => _icon = value;
  }

  /// <summary>
  /// Whether this tag proficiency is available for use
  /// </summary>
  public bool IsActive {
    get => _isActive;
    set => _isActive = value;
  }

  // Navigation properties
  public virtual ICollection<Certificate.Models.CertificateTag> CertificateTags {
    get => _certificateTags;
    set => _certificateTags = value;
  }
}
