using GameGuild.Common;


namespace GameGuild.Modules.Programs.Interfaces;

/// <summary>
/// Interface for program management services
/// </summary>
public interface IProgramService {
  Task<Models.Program> CreateProgramAsync(Models.Program program);

  Task<Models.Program?> GetProgramByIdAsync(int id);

  Task<IEnumerable<Models.Program>> GetProgramsByVisibilityAsync(Visibility visibility);

  Task<Models.Program> UpdateProgramAsync(Models.Program program);

  Task<bool> DeleteProgramAsync(int id);

  Task<IEnumerable<Models.Program>> GetProgramsByProductAsync(int productId);

  Task<bool> AddProgramToProductAsync(int productId, int programId, int sortOrder = 0);

  Task<bool> RemoveProgramFromProductAsync(int productId, int programId);
}

/// <summary>
/// Interface for program content management services
/// </summary>
public interface IProgramContentService {
  Task<Models.ProgramContent> CreateContentAsync(Models.ProgramContent content);

  Task<Models.ProgramContent?> GetContentByIdAsync(Guid id);

  Task<IEnumerable<Models.ProgramContent>> GetContentByProgramAsync(Guid programId);

  Task<IEnumerable<Models.ProgramContent>> GetContentByParentAsync(Guid parentId);

  Task<IEnumerable<Models.ProgramContent>> GetTopLevelContentAsync(Guid programId);

  Task<Models.ProgramContent> UpdateContentAsync(Models.ProgramContent content);

  Task<bool> DeleteContentAsync(Guid id);

  Task<bool> ReorderContentAsync(Guid programId, List<(Guid contentId, int sortOrder)> newOrder);

  Task<IEnumerable<Models.ProgramContent>> GetRequiredContentAsync(Guid programId);

  Task<IEnumerable<Models.ProgramContent>> GetContentByTypeAsync(
    Guid programId,
    ProgramContentType type
  );

  Task<IEnumerable<Models.ProgramContent>> GetContentByVisibilityAsync(
    Guid programId,
    Visibility visibility
  );

  Task<bool> MoveContentAsync(Guid contentId, Guid? newParentId, int newSortOrder);

  Task<int> GetContentCountAsync(Guid programId);

  Task<int> GetRequiredContentCountAsync(Guid programId);

  Task<IEnumerable<Models.ProgramContent>> SearchContentAsync(Guid programId, string searchTerm);
}

/// <summary>
/// Interface for program user enrollment and progress services
/// </summary>
public interface IProgramUserService {
  Task<Models.ProgramUser> EnrollUserAsync(int userProductId, int programId);

  Task<Models.ProgramUser?> GetProgramUserAsync(int id);

  Task<Models.ProgramUser?> GetProgramUserAsync(int userId, int programId);

  Task<IEnumerable<Models.ProgramUser>> GetUserProgramsAsync(int userId);

  Task<Models.ProgramUser> UpdateProgressAsync(int programUserId, decimal completionPercentage);

  Task<Models.ProgramUser> UpdateGradeAsync(int programUserId, decimal finalGrade);

  Task<bool> CompleteeProgramAsync(int programUserId);

  Task<IEnumerable<Models.ProgramUser>> GetProgramEnrollmentsAsync(int programId);

  Task<bool> UnenrollUserAsync(int programUserId);
}

/// <summary>
/// Interface for content interaction tracking services
/// </summary>
public interface IContentInteractionService {
  Task<Models.ContentInteraction> StartContentAsync(Guid programUserId, Guid contentId);

  Task<Models.ContentInteraction> UpdateProgressAsync(Guid interactionId, decimal completionPercentage);

  Task<Models.ContentInteraction> SubmitContentAsync(Guid interactionId, string submissionData);

  Task<Models.ContentInteraction> CompleteContentAsync(Guid interactionId);

  Task<Models.ContentInteraction?> GetInteractionAsync(Guid programUserId, Guid contentId);

  Task<IEnumerable<Models.ContentInteraction>> GetUserInteractionsAsync(Guid programUserId);

  Task<Models.ContentInteraction> UpdateTimeSpentAsync(Guid interactionId, int additionalMinutes);
}

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
