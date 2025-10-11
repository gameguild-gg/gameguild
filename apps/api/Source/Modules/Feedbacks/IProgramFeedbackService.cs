using GameGuild.Modules.Feedbacks.Entities;

namespace GameGuild.Modules.Feedbacks;

/// <summary>
/// Interface for program feedback services
/// </summary>
public interface IProgramFeedbackService
{
    Task<ProgramFeedbackSubmission> SubmitFeedbackAsync(ProgramFeedbackSubmission feedback);

    Task<ProgramFeedbackSubmission?> GetFeedbackByIdAsync(Guid id);

    Task<IEnumerable<ProgramFeedbackSubmission>> GetProgramFeedbackAsync(Guid programId);

    Task<ProgramFeedbackSubmission?> GetUserFeedbackAsync(Guid userId, Guid programId);

    Task<bool> HasUserSubmittedFeedbackAsync(Guid userId, Guid programId);

    Task<ProgramFeedbackSubmission> UpdateFeedbackAsync(ProgramFeedbackSubmission feedback);

    Task<bool> DeleteFeedbackAsync(Guid id);

    Task<IEnumerable<ProgramFeedbackSubmission>> GetFeedbackByCategoryAsync(FeedbackCategory category);

    Task<IEnumerable<ProgramFeedbackSubmission>> GetAnonymousFeedbackAsync(Guid programId);
}
