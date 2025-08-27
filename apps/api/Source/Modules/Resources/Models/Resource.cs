using GameGuild.Common;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Localization;
using GameGuild.Modules.Tenants;


namespace GameGuild.Modules.Resources;

/// <summary>
/// Abstract base class for resources that can have permissions and be localized.
/// Provides common functionality for content resources like articles, courses, media, etc.
/// </summary>
public abstract class Resource : Entity, ILocalizable, ITenantable {
  /// <summary>
  /// The title/name of this resource
  /// </summary>
  [Required]
  [MaxLength(255)]
  public string Title { get; set; } = string.Empty;

  /// <summary>
  /// Optional description of this resource
  /// </summary>
  [MaxLength(2000)]
  public string? Description { get; set; }

  /// <summary>
  /// Access level of the resource (Public, Private, Restricted, etc.)
  /// </summary>
  [Required]
  public AccessLevel Visibility { get; set; } = AccessLevel.Private;

  /// <summary>
  /// Navigation property to resource metadata
  /// Entity Framework will automatically create the MetadataId foreign key
  /// </summary>
  public virtual ResourceMetadata? Metadata { get; set; }

  /// <summary>
  /// Navigation property to the tenant this resource belongs to (implements ITenantable)
  /// Entity Framework will automatically create the TenantId foreign key
  /// Null means the resource is global and accessible across all tenants
  /// </summary>
  public override Tenant? Tenant { get; set; }

  /// <summary>
  /// Indicates whether this resource is accessible across all tenants (when Tenant is null)
  /// or only within a specific tenant (implements ITenantable)
  /// </summary>
  public override bool IsGlobal {
    get => Tenant == null;
  }

  // todo: this could be better implemented if it was an enum
  // todo: suggestion: the unicity of the resource is given by the lang and the resourceid. resourceid is not the id of the resource. we will filter the latest version of the resourceid and the lang it is requested
  /// <summary>
  /// Collection of localizations for this resource
  /// </summary>
  public virtual ICollection<ResourceLocalization> Localizations { get; set; } = new List<ResourceLocalization>();

  /// <summary>
  /// Adds a localization for a specific field in the given language
  /// </summary>
  public virtual ResourceLocalization AddLocalization(
    string fieldName, string content, Language language,
    LocalizationStatus status = LocalizationStatus.Draft
  ) {
    var localization = new ResourceLocalization {
      ResourceType = GetType().Name,
      Language = language,
      FieldName = fieldName,
      Content = content,
      Status = status,
    };

    Localizations.Add(localization);

    return localization;
  }

  /// <summary>
  /// Assigns this resource to a specific tenant
  /// </summary>
  /// <param name="tenant">The tenant to assign this resource to</param>
  public void AssignToTenant(Tenant tenant) { Tenant = tenant; }

  /// <summary>
  /// Makes this resource global (accessible across all tenants)
  /// </summary>
  public void MakeGlobal() { Tenant = null; }
}
