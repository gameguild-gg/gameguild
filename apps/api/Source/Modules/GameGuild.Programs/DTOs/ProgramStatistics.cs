namespace GameGuild.Modules.Programs.DTOs;

public record ProgramStatistics(
    Guid ProgramId,
    int TotalEnrollments,
    int ActiveEnrollments,
    int CompletedEnrollments,
    decimal AverageRating,
    int TotalRatings,
    decimal CompletionRate,
    TimeSpan AverageCompletionTime);