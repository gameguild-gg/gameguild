namespace GameGuild.Learning.Courses;

/// <summary>
/// Extension methods for mapping Program entities to DTOs.
/// </summary>
public static class ProgramMappingExtensions
{
    /// <summary>Maps a Program entity to a ProgramDto, excluding navigation properties and internal audit fields.</summary>
    public static ProgramDto ToDto(this Program program)
    {
        return new ProgramDto
        {
            Id = program.Id,
            CreatorId = program.CreatorId,
            Title = program.Title,
            Description = program.Description,
            Metadata = program.Metadata,
            Visibility = program.Visibility,
            Slug = program.Slug,
            Status = program.Status,
            Thumbnail = program.Thumbnail,
            VideoShowcaseUrl = program.VideoShowcaseUrl,
            EstimatedHours = program.EstimatedHours,
            PassingScore = program.PassingScore,
            EnrollmentStatus = program.EnrollmentStatus,
            MaxEnrollments = program.MaxEnrollments,
            EnrollmentDeadline = program.EnrollmentDeadline,
            Category = program.Category,
            Difficulty = program.Difficulty,
            SkillsRequired = program.SkillsRequired,
            SkillsProvided = program.SkillsProvided,
            CurrentEnrollments = program.CurrentEnrollments,
            AverageRating = program.AverageRating,
            TotalRatings = program.TotalRatings,
            IsEnrollmentOpen = program.IsEnrollmentOpen,
            CreatedAt = program.CreatedAt,
            UpdatedAt = program.UpdatedAt,
        };
    }

    /// <summary>Maps a collection of Program entities to ProgramDto collection.</summary>
    public static IEnumerable<ProgramDto> ToDtos(this IEnumerable<Program> programs)
    {
        return programs.Select(p => p.ToDto());
    }
}
