using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.Program.DTOs;

/// <summary>
/// DTO for ActivityGrade responses - avoids circular references for Swagger/OpenAPI
/// </summary>
public class ActivityGradeDto
{
    public Guid Id { get; set; }
    public Guid ContentInteractionId { get; set; }
    public Guid GraderProgramUserId { get; set; }
    public decimal Grade { get; set; }
    public string? Feedback { get; set; }
    public string? GradingDetails { get; set; }
    public DateTime GradedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Simplified nested objects to avoid circular references
    public ContentInteractionSummaryDto? ContentInteraction { get; set; }
    public GraderSummaryDto? Grader { get; set; }

    // Computed properties for convenience
    public bool IsPassingGrade => Grade >= 70; // Assuming 70% is passing
    public string GradePercentage => $"{Grade:F1}%";
    public bool HasFeedback => !string.IsNullOrEmpty(Feedback);
    public bool HasGradingDetails => !string.IsNullOrEmpty(GradingDetails);
}

/// <summary>
/// Simplified content interaction information to avoid circular references
/// </summary>
public class ContentInteractionSummaryDto
{
    public Guid Id { get; set; }
    public Guid ProgramUserId { get; set; }
    public Guid ContentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public ContentSummaryDto? Content { get; set; }
    public StudentSummaryDto? Student { get; set; }
}

/// <summary>
/// Simplified student information to avoid circular references
/// </summary>
public class StudentSummaryDto
{
    public Guid Id { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
}

/// <summary>
/// Simplified grader information to avoid circular references
/// </summary>
public class GraderSummaryDto
{
    public Guid Id { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// DTO for creating new activity grades
/// </summary>
public record CreateActivityGradeDto(
    [Required] Guid ContentInteractionId,
    [Required] Guid GraderProgramUserId,
    [Required] [Range(0, 100)] decimal Grade,
    string? Feedback = null,
    string? GradingDetails = null
);

/// <summary>
/// DTO for updating existing activity grades
/// </summary>
public record UpdateActivityGradeDto(
    [Range(0, 100)] decimal? Grade = null,
    string? Feedback = null,
    string? GradingDetails = null
);

/// <summary>
/// DTO for grade statistics responses
/// </summary>
public class GradeStatisticsDto
{
    public int TotalGrades { get; set; }
    public decimal AverageGrade { get; set; }
    public decimal MinGrade { get; set; }
    public decimal MaxGrade { get; set; }
    public decimal PassingRate { get; set; }
    
    // Additional computed properties for better UX
    public string AverageGradeFormatted => $"{AverageGrade:F1}%";
    public string PassingRateFormatted => $"{PassingRate:F1}%";
    public bool HasGrades => TotalGrades > 0;
}

/// <summary>
/// Extension methods to convert from entity/service models to DTOs
/// </summary>
public static class ActivityGradeExtensions
{
    public static ActivityGradeDto ToDto(this Models.ActivityGrade grade)
    {
        return new ActivityGradeDto
        {
            Id = grade.Id,
            ContentInteractionId = grade.ContentInteractionId,
            GraderProgramUserId = grade.GraderProgramUserId,
            Grade = grade.Grade,
            Feedback = grade.Feedback,
            GradingDetails = grade.GradingDetails,
            GradedAt = grade.GradedAt,
            CreatedAt = grade.CreatedAt,
            UpdatedAt = grade.UpdatedAt,
            ContentInteraction = grade.ContentInteraction != null ? new ContentInteractionSummaryDto
            {
                Id = grade.ContentInteraction.Id,
                ProgramUserId = grade.ContentInteraction.ProgramUserId,
                ContentId = grade.ContentInteraction.ContentId,
                Status = grade.ContentInteraction.Status.ToString(),
                SubmittedAt = grade.ContentInteraction.SubmittedAt,
                Content = grade.ContentInteraction.Content != null ? new ContentSummaryDto
                {
                    Id = grade.ContentInteraction.Content.Id,
                    Title = grade.ContentInteraction.Content.Title,
                    ContentType = grade.ContentInteraction.Content.Type.ToString(),
                    EstimatedMinutes = grade.ContentInteraction.Content.EstimatedMinutes
                } : null,
                Student = grade.ContentInteraction.ProgramUser?.User != null ? new StudentSummaryDto
                {
                    Id = grade.ContentInteraction.ProgramUser.User.Id,
                    UserDisplayName = grade.ContentInteraction.ProgramUser.User.Name,
                    UserEmail = grade.ContentInteraction.ProgramUser.User.Email
                } : null
            } : null,
            Grader = grade.GraderProgramUser?.User != null ? new GraderSummaryDto
            {
                Id = grade.GraderProgramUser.User.Id,
                UserDisplayName = grade.GraderProgramUser.User.Name,
                UserEmail = grade.GraderProgramUser.User.Email,
                Role = "Grader" // Default role since ProgramUser doesn't have a Role property
            } : null
        };
    }

    public static IEnumerable<ActivityGradeDto> ToDto(this IEnumerable<Models.ActivityGrade> grades)
    {
        return grades.Select(g => g.ToDto());
    }

    public static GradeStatisticsDto ToDto(this Services.GradeStatistics statistics)
    {
        return new GradeStatisticsDto
        {
            TotalGrades = statistics.TotalGrades,
            AverageGrade = statistics.AverageGrade,
            MinGrade = statistics.MinGrade,
            MaxGrade = statistics.MaxGrade,
            PassingRate = statistics.PassingRate
        };
    }
}
