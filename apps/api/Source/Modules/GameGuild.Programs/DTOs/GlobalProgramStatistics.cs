namespace GameGuild.Modules.Programs.DTOs;

public record GlobalProgramStatistics(
    int TotalPrograms,
    int PublishedPrograms,
    int TotalEnrollments,
    int ActiveEnrollments,
    decimal AverageRating,
    int TotalRatings,
    ProgramCategory? MostPopularCategory,
    ProgramDifficulty? MostPopularDifficulty);