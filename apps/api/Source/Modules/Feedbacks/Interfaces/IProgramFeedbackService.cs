namespace GameGuild.Modules.Feedbacks;

/// <summary>
/// Interface for program feedback services
/// </summary>
public interface IProgramFeedbackService {
  Task<ProgramFeedbackSubmission> SubmitFeedbackAsync(ProgramFeedbackSubmission feedback);

  Task<ProgramFeedbackSubmission?> GetFeedbackByIdAsync(int id);

  Task<IEnumerable<ProgramFeedbackSubmission>> GetProgramFeedbackAsync(int programId);

  Task<ProgramFeedbackSubmission?> GetUserFeedbackAsync(int userId, int programId);

  Task<bool> HasUserSubmittedFeedbackAsync(int userId, int programId);

  Task<ProgramFeedbackSubmission> UpdateFeedbackAsync(ProgramFeedbackSubmission feedback);

  Task<bool> DeleteFeedbackAsync(int id);
}
