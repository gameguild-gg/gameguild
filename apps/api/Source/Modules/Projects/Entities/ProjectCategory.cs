namespace GameGuild.Modules.Projects.Entities;

/// <summary> Represents a project category (game, tool, art, etc.) </summary>
[Index(nameof(Name), IsUnique = true)]
public class ProjectCategory : EntityBase<Guid>
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary> Projects in this category </summary>
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
