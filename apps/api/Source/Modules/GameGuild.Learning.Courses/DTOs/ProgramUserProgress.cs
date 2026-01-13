namespace GameGuild.Learning.Courses;

public record ProgramUserProgress(
    Guid ProgramId,
    Guid UserId,
    int CompletedContent,
    int TotalContent,
    decimal ProgressPercentage,
    TimeSpan TimeSpent,
    DateTime? LastActivityAt,
    bool IsCompleted,
    DateTime? CompletedAt);