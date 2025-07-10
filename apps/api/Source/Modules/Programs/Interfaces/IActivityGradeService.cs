namespace GameGuild.Modules.Programs.Interfaces;

/// <summary>
/// Interface for activity grading services
/// </summary>
public interface IActivityGradeService {
  Task<Models.ActivityGrade> GradeActivityAsync(
    Guid contentInteractionId,
    Guid graderProgramUserId,
    decimal grade,
    string? feedback = null,
    string? gradingDetails = null
  );

  Task<Models.ActivityGrade?> GetGradeAsync(Guid contentInteractionId);

  Task<Models.ActivityGrade?> GetGradeByIdAsync(Guid gradeId);

  Task<IEnumerable<Models.ActivityGrade>> GetGradesByGraderAsync(Guid graderProgramUserId);

  Task<IEnumerable<Models.ActivityGrade>> GetGradesByStudentAsync(Guid programUserId);

  Task<Models.ActivityGrade?> UpdateGradeAsync(Guid gradeId, decimal? newGrade = null, string? newFeedback = null, string? newGradingDetails = null);

  Task<bool> DeleteGradeAsync(Guid gradeId);

  Task<IEnumerable<Models.ContentInteraction>> GetPendingGradesAsync(Guid programId);

  Task<Services.GradeStatistics> GetGradeStatisticsAsync(Guid programId);

  Task<IEnumerable<Models.ActivityGrade>> GetGradesByContentAsync(Guid contentId);
}
