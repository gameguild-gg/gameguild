using System.ComponentModel.DataAnnotations;
using GameGuild.Modules.Resources.Models;


namespace GameGuild.Modules.Contents.Models;

/// <summary>
/// Represents a license that can be assigned to content.
/// </summary>
public class ContentLicense : ResourceBase {
  /// <summary>
  /// Optional URL to the license text
  /// </summary>
  [MaxLength(500)]
  public string? Url { get; set; }

  /// <summary>
  /// Navigation property to content items using this license
  /// </summary>
  public virtual ICollection<Content> Contents { get; set; } = new List<Content>();
}
