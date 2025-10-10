using GameGuild.Modules.Feedbacks.Entities;

namespace GameGuild.Modules.Feedbacks;

/// <summary>
/// Interface for program rating services
/// </summary>
public interface IProgramRatingService
{
    Task<ProgramRating> SubmitRatingAsync(ProgramRating rating);

    Task<ProgramRating?> GetRatingByIdAsync(Guid id);

    Task<IEnumerable<ProgramRating>> GetProgramRatingsAsync(Guid programId);

    Task<ProgramRating?> GetUserRatingAsync(Guid userId, Guid programId);

    Task<bool> HasUserSubmittedRatingAsync(Guid userId, Guid programId);

    Task<ProgramRating> UpdateRatingAsync(ProgramRating rating);

    Task<ProgramRating> ModerateRatingAsync(Guid ratingId, Guid moderatorId, ModerationStatus status, string? notes = null);

    Task<decimal> GetAverageRatingAsync(Guid programId);

    Task<IEnumerable<ProgramRating>> GetPendingModerationAsync();

    Task<bool> DeleteRatingAsync(Guid id);

    Task<Dictionary<int, int>> GetRatingDistributionAsync(Guid programId);
}
