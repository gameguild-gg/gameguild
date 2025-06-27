using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Project.Models;

/// <summary>
/// Represents a project category (game, tool, art, etc.)
/// </summary>
public class ProjectCategory : ResourceBase {
  private string _name = string.Empty;

  private ICollection<Project> _projects = new List<Project>();

  [Required]
  [MaxLength(50)]
  public string Name {
    get => _name;
    set => _name = value;
  }

  /// <summary>
  /// Projects in this category
  /// </summary>
  public virtual ICollection<Project> Projects {
    get => _projects;
    set => _projects = value;
  }
}
