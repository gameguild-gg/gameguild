namespace GameGuild.Modules.Programs;

/// <summary> Content completion statistics </summary>
public class ContentCompletionStats {
    public int TotalContentItems { get; set; }

    public int CompletedContentItems { get; set; }

    public int InProgressContentItems { get; set; }

    public int NotStartedContentItems { get; set; }

    public decimal AverageCompletionRate { get; set; }

    public decimal AverageScore { get; set; }

    public int TotalTimeSpentHours { get; set; }

    public Dictionary<string, int> CompletionByContentType { get; set; } = new Dictionary<string, int>();
}