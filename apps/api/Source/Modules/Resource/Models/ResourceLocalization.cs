using System.ComponentModel.DataAnnotations;

namespace GameGuild.Common.Entities;

/// <summary>
/// Entity for storing localized content for resources
/// Provides multi-language support for resources
/// </summary>
/// todo: field should be the one coming from the resource as a generic origin, and not a plain string. revist the others entries too, such as resource type.
public class ResourceLocalization : BaseEntity {
  private string _resourceType = string.Empty;

  private Language _language = null!;

  private string _fieldName = string.Empty;

  private Guid _resourceId;

  private string _content = string.Empty;

  private bool _isDefault = false;

  private LocalizationStatus _status = LocalizationStatus.Draft;

  // todo: apply polymorphism
  /// <summary>
  /// Type of the resource being localized
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string ResourceType {
    get => _resourceType;
    set => _resourceType = value;
  }

  /// <summary>
  /// Navigation property to the language
  /// Entity Framework will automatically create the LanguageId foreign key
  /// </summary>
  [Required]
  public virtual Language Language {
    get => _language;
    set => _language = value;
  }

  /// <summary>
  /// The field name being localized (e.g., 'Title', 'Description', 'Content')
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string FieldName {
    get => _fieldName;
    set => _fieldName = value;
  }

  /// <summary>
  /// Foreign key to the Resource entity
  /// </summary>
  [Required]
  public Guid ResourceId {
    get => _resourceId;
    set => _resourceId = value;
  }

  /// <summary>
  /// The localized content
  /// </summary>
  [Required]
  public string Content {
    get => _content;
    set => _content = value;
  }

  /// <summary>
  /// Whether this localization is the default for the language
  /// </summary>
  public bool IsDefault {
    get => _isDefault;
    set => _isDefault = value;
  }

  /// <summary>
  /// Status of the localization (Draft, Published, NeedsReview, etc.)
  /// </summary>
  public LocalizationStatus Status {
    get => _status;
    set => _status = value;
  }
}
