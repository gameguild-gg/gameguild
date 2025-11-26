namespace GameGuild.Modules.Programs.DTOs;

public record CreatorProgramStatistics(
    Guid CreatorId,
    int TotalPrograms,
    int PublishedPrograms,
    int TotalEnrollments,
    int ActiveEnrollments,
    decimal AverageRating,
    int TotalRatings,
    decimal AverageCompletionRate);