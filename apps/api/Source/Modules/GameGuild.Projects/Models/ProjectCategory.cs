using System.ComponentModel.DataAnnotations;
using GameGuild;
using GameGuild.Projects.Entities;

namespace GameGuild.Projects.Models;

/// <summary> Represents a project category (game, tool, art, etc.) </summary>
public class ProjectCategory : EntityBase<Guid> {
  [Required][MaxLength(50)] public string Name { get; set; } = string.Empty;

  /// <summary> Projects in this category </summary>
  public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
