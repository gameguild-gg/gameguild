namespace GameGuild.Modules.Programs.DTOs;

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