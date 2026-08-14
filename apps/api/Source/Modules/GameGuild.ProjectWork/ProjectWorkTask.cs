using System.ComponentModel.DataAnnotations;
using GameGuild.Identity.Users;

namespace GameGuild.ProjectWork;

public sealed class ProjectWorkTask : EntityBase
{
    public Guid ProjectId { get; set; }
    public Guid BoardId { get; set; }
    public Guid ColumnId { get; set; }
    public ProjectWorkColumn? Column { get; set; }
    public Guid? MilestoneId { get; set; }
    public ProjectMilestone? Milestone { get; set; }

    [Required, MaxLength(300)] public string Title { get; set; } = string.Empty;
    [MaxLength(10000)] public string? Description { get; set; }
    public ProjectWorkTaskStatus Status { get; set; } = ProjectWorkTaskStatus.Backlog;
    public ProjectWorkTaskPriority Priority { get; set; } = ProjectWorkTaskPriority.Normal;
    public Guid? AssigneeUserId { get; set; }
    public User? AssigneeUser { get; set; }
    public Guid CreatedByUserId { get; set; }
    public int Position { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ICollection<ProjectTaskChecklistItem> Checklist { get; set; } = new List<ProjectTaskChecklistItem>();
    public ICollection<ProjectTaskComment> Comments { get; set; } = new List<ProjectTaskComment>();

    public void Complete(IEnumerable<ProjectWorkTask> dependencies)
    {
        if (dependencies.Any(task => task.Status != ProjectWorkTaskStatus.Done))
            throw new InvalidOperationException("A blocked task cannot be completed.");
        Status = ProjectWorkTaskStatus.Done;
        CompletedAt = SystemClock.UtcNow;
        Touch();
    }
}

public sealed class ProjectTaskChecklistItem : EntityBase
{
    public Guid TaskId { get; set; }
    public ProjectWorkTask Task { get; set; } = null!;
    [Required, MaxLength(500)] public string Text { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int Position { get; set; }
}

public sealed class ProjectTaskComment : EntityBase
{
    public Guid TaskId { get; set; }
    public ProjectWorkTask Task { get; set; } = null!;
    public Guid AuthorUserId { get; set; }
    [Required, MaxLength(10000)] public string Body { get; set; } = string.Empty;
    public DateTime? EditedAt { get; set; }
}
