namespace GameGuild.Modules.Programs;

/// <summary>
/// Interface for activity grading services
/// </summary>
public interface IActivityGradeService {
  Task<ActivityGrade> GradeActivityAsync(
    Guid contentInteractionId,
    Guid graderProgramUserId,
    decimal grade,
    string? feedback = null,
    string? gradingDetails = null
  );

  Task<ActivityGrade?> GetGradeAsync(Guid contentInteractionId);

  Task<ActivityGrade?> GetGradeByIdAsync(Guid gradeId);

  Task<IEnumerable<ActivityGrade>> GetGradesByGraderAsync(Guid graderProgramUserId);

  Task<IEnumerable<ActivityGrade>> GetGradesByStudentAsync(Guid programUserId);

  Task<ActivityGrade?> UpdateGradeAsync(Guid gradeId, decimal? newGrade = null, string? newFeedback = null, string? newGradingDetails = null);

  Task<bool> DeleteGradeAsync(Guid gradeId);

  Task<IEnumerable<ContentInteraction>> GetPendingGradesAsync(Guid programId);

  Task<GradeStatistics> GetGradeStatisticsAsync(Guid programId);

  Task<IEnumerable<ActivityGrade>> GetGradesByContentAsync(Guid contentId);
}
