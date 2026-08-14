using System.ComponentModel.DataAnnotations;

namespace GameGuild.ProjectWork;

public sealed class ProjectBoard : EntityBase
{
    public Guid ProjectId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = "Project Board";

    public ICollection<ProjectWorkColumn> Columns { get; set; } = new List<ProjectWorkColumn>();

    public static ProjectBoard Create(Guid tenantId, Guid projectId)
    {
        var board = new ProjectBoard { TenantId = tenantId, ProjectId = projectId };
        var defaults = new[]
        {
            ("Backlog", ProjectWorkColumnKind.Backlog),
            ("Ready", ProjectWorkColumnKind.Ready),
            ("In Progress", ProjectWorkColumnKind.InProgress),
            ("In Review", ProjectWorkColumnKind.InReview),
            ("Done", ProjectWorkColumnKind.Done)
        };
        for (var index = 0; index < defaults.Length; index++)
            board.Columns.Add(new ProjectWorkColumn
            {
                TenantId = tenantId,
                BoardId = board.Id,
                Name = defaults[index].Item1,
                Kind = defaults[index].Item2,
                Position = index
            });
        return board;
    }
}

public sealed class ProjectWorkColumn : EntityBase
{
    public Guid BoardId { get; set; }
    public ProjectBoard Board { get; set; } = null!;
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    public ProjectWorkColumnKind Kind { get; set; } = ProjectWorkColumnKind.Custom;
    public int Position { get; set; }
    public int? WorkInProgressLimit { get; set; }
    public ICollection<ProjectWorkTask> Tasks { get; set; } = new List<ProjectWorkTask>();
}
