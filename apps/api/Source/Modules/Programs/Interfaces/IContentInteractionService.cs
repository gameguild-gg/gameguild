namespace GameGuild.Modules.Programs.Interfaces;

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
