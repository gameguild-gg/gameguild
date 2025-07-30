using GameGuild.Common;
using GameGuild.Modules.Programs;


namespace GameGuild.Modules.Feedbacks;

/// <summary>
/// Interface for program rating services
/// </summary>
public interface IProgramRatingService {
  Task<ProgramRating> SubmitRatingAsync(ProgramRating rating);

  Task<ProgramRating?> GetRatingByIdAsync(int id);

  Task<IEnumerable<ProgramRating>> GetProgramRatingsAsync(int programId);

  Task<ProgramRating?> GetUserRatingAsync(int userId, int programId);

  Task<bool> HasUserSubmittedRatingAsync(int userId, int programId);

  Task<ProgramRating> UpdateRatingAsync(ProgramRating rating);

  Task<ProgramRating> ModerateRatingAsync(int ratingId, int moderatorId, ModerationStatus status);

  Task<decimal> GetAverageRatingAsync(int programId);

  Task<IEnumerable<ProgramRating>> GetPendingModerationAsync();

  Task<bool> DeleteRatingAsync(int id);
}
