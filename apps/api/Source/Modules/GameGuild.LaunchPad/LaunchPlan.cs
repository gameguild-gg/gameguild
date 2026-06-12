using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Projects;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.LaunchPad;

[Table("launch_plans")]
[Index(nameof(ProjectId), IsUnique = true, Name = "IX_launch_plans_ProjectId")]
[Index(nameof(Status), nameof(TargetLaunchAt), Name = "IX_launch_plans_Status_TargetLaunchAt")]
public sealed class LaunchPlan : EntityBase<Guid>
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Positioning { get; set; }

    public DateTime? TargetLaunchAt { get; set; }
    public DateTime? LaunchedAt { get; set; }
    public LaunchPlanStatus Status { get; set; } = LaunchPlanStatus.Preparing;
    public string[] Channels { get; set; } = [];
    public ICollection<LaunchChecklistItem> ChecklistItems { get; set; } = new List<LaunchChecklistItem>();

    [NotMapped]
    public int ReadinessPercent
    {
        get
        {
            if (ChecklistItems.Count == 0) return 0;
            return (int)Math.Round(ChecklistItems.Count(item => item.IsComplete) * 100m / ChecklistItems.Count);
        }
    }

    public void RecalculateStatus()
    {
        if (Status == LaunchPlanStatus.Launched || Status == LaunchPlanStatus.Paused) return;
        Status = ReadinessPercent == 100 ? LaunchPlanStatus.Ready : LaunchPlanStatus.Preparing;
    }

    public void Publish()
    {
        RecalculateStatus();
        if (Status != LaunchPlanStatus.Ready) throw new InvalidOperationException("Launch plan must be ready before publishing.");

        Status = LaunchPlanStatus.Launched;
        LaunchedAt = SystemClock.UtcNow;
    }
}

public sealed class LaunchChecklistItem : EntityBase<Guid>
{
    public Guid LaunchPlanId { get; set; }
    public LaunchPlan LaunchPlan { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = "Readiness";

    public bool IsRequired { get; set; } = true;
    public bool IsComplete { get; set; }
    public DateTime? CompletedAt { get; set; }

    public void Complete()
    {
        IsComplete = true;
        CompletedAt = SystemClock.UtcNow;
    }
}

public enum LaunchPlanStatus
{
    Draft = 0,
    Preparing = 1,
    Ready = 2,
    Launched = 3,
    Paused = 4
}
