using System.ComponentModel.DataAnnotations;
using GameGuild.Modules.Resources;


namespace GameGuild.Modules.Projects.Models;

/// <summary>
/// Represents a project category (game, tool, art, etc.)
/// </summary>
public class ProjectCategory : Resource {
  [Required] [MaxLength(50)] public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Projects in this category
  /// </summary>
  public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
