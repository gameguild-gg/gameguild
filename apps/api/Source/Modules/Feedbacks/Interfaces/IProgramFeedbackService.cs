namespace GameGuild.Modules.Feedbacks.Interfaces;

/// <summary>
/// Interface for program feedback services
/// </summary>
public interface IProgramFeedbackService {
  Task<Models.ProgramFeedbackSubmission> SubmitFeedbackAsync(Models.ProgramFeedbackSubmission feedback);

  Task<Models.ProgramFeedbackSubmission?> GetFeedbackByIdAsync(int id);

  Task<IEnumerable<Models.ProgramFeedbackSubmission>> GetProgramFeedbackAsync(int programId);

  Task<Models.ProgramFeedbackSubmission?> GetUserFeedbackAsync(int userId, int programId);

  Task<bool> HasUserSubmittedFeedbackAsync(int userId, int programId);

  Task<Models.ProgramFeedbackSubmission> UpdateFeedbackAsync(Models.ProgramFeedbackSubmission feedback);

  Task<bool> DeleteFeedbackAsync(int id);
}
