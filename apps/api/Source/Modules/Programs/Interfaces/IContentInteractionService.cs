namespace GameGuild.Modules.Programs;

/// <summary>
/// Interface for content interaction tracking services
/// </summary>
public interface IContentInteractionService {
  Task<ContentInteraction> StartContentAsync(Guid programUserId, Guid contentId);

  Task<ContentInteraction> UpdateProgressAsync(Guid interactionId, decimal completionPercentage);

  Task<ContentInteraction> SubmitContentAsync(Guid interactionId, string submissionData);

  Task<ContentInteraction> CompleteContentAsync(Guid interactionId);

  Task<ContentInteraction?> GetInteractionAsync(Guid programUserId, Guid contentId);

  Task<IEnumerable<ContentInteraction>> GetUserInteractionsAsync(Guid programUserId);

  Task<ContentInteraction> UpdateTimeSpentAsync(Guid interactionId, int additionalMinutes);
}
