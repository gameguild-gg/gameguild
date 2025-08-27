using GameGuild.Common;
using GameGuild.Database;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Service for managing content interactions following the permission inheritance pattern
/// ContentInteraction inherits permissions from Program -> ProgramContent -> ContentInteraction
/// Once submitted, interactions become immutable but users can create new interactions
/// </summary>
public class ContentInteractionService(ApplicationDbContext context) : IContentInteractionService {
  /// <summary>
  /// Start a new content interaction (or resume existing one if not submitted)
  /// </summary>
  public async Task<ContentInteraction> StartContentAsync(Guid programUserId, Guid contentId) {
    // Check if there's already an interaction for this user/content
    var existingInteraction = await context.ContentInteractions
                                           .FirstOrDefaultAsync(ci => ci.ProgramUserId == programUserId && ci.ContentId == contentId);

    if (existingInteraction != null) {
      // If already submitted, create a new interaction based on the last one
      if (existingInteraction.SubmittedAt.HasValue) return await CreateNewInteractionFromPreviousAsync(existingInteraction);

      // Otherwise, resume the existing interaction
      existingInteraction.FirstAccessedAt ??= DateTime.UtcNow;
      existingInteraction.LastAccessedAt = DateTime.UtcNow;
      existingInteraction.Status = ProgressStatus.InProgress;

      await context.SaveChangesAsync();

      return existingInteraction;
    }

    // Create new interaction
    var newInteraction = new ContentInteraction {
      ProgramUserId = programUserId,
      ContentId = contentId,
      Status = ProgressStatus.InProgress,
      FirstAccessedAt = DateTime.UtcNow,
      LastAccessedAt = DateTime.UtcNow,
      CompletionPercentage = 0,
    };

    context.ContentInteractions.Add(newInteraction);
    await context.SaveChangesAsync();

    return newInteraction;
  }

  /// <summary>
  /// Update progress for an interaction (only if not submitted)
  /// </summary>
  public async Task<ContentInteraction> UpdateProgressAsync(Guid interactionId, decimal completionPercentage) {
    var interaction = await GetInteractionByIdAsync(interactionId);

    if (interaction.SubmittedAt.HasValue) throw new InvalidOperationException("Cannot update progress on submitted interaction. Create a new interaction to continue work.");

    interaction.CompletionPercentage = Math.Min(100, Math.Max(0, completionPercentage));
    interaction.LastAccessedAt = DateTime.UtcNow;

    if (interaction.CompletionPercentage >= 100) {
      interaction.Status = ProgressStatus.Completed;
      interaction.CompletedAt = DateTime.UtcNow;
    }
    else if (interaction.Status == ProgressStatus.NotStarted) { interaction.Status = ProgressStatus.InProgress; }

    await context.SaveChangesAsync();

    return interaction;
  }

  /// <summary>
  /// Submit content interaction (makes it immutable)
  /// </summary>
  public async Task<ContentInteraction> SubmitContentAsync(Guid interactionId, string submissionData) {
    var interaction = await GetInteractionByIdAsync(interactionId);

    if (interaction.SubmittedAt.HasValue) throw new InvalidOperationException("Interaction has already been submitted and cannot be changed.");

    interaction.SubmissionData = submissionData;
    interaction.SubmittedAt = DateTime.UtcNow;
    interaction.LastAccessedAt = DateTime.UtcNow;
    interaction.Status = ProgressStatus.Completed; // Use Completed instead of Submitted
    interaction.CompletedAt = DateTime.UtcNow;
    interaction.CompletionPercentage = 100;

    await context.SaveChangesAsync();

    return interaction;
  }

  /// <summary>
  /// Mark content as completed
  /// </summary>
  public async Task<ContentInteraction> CompleteContentAsync(Guid interactionId) {
    var interaction = await GetInteractionByIdAsync(interactionId);

    if (interaction.SubmittedAt.HasValue) throw new InvalidOperationException("Cannot modify submitted interaction. Create a new interaction to continue work.");

    interaction.Status = ProgressStatus.Completed;
    interaction.CompletedAt = DateTime.UtcNow;
    interaction.LastAccessedAt = DateTime.UtcNow;
    interaction.CompletionPercentage = 100;

    await context.SaveChangesAsync();

    return interaction;
  }

  /// <summary>
  /// Get interaction for a specific user and content
  /// </summary>
  public async Task<ContentInteraction?> GetInteractionAsync(Guid programUserId, Guid contentId) {
    return await context.ContentInteractions
                        .Include(ci => ci.ProgramUser)
                        .Include(ci => ci.Content)
                        .Include(ci => ci.ActivityGrades)
                        .FirstOrDefaultAsync(ci => ci.ProgramUserId == programUserId && ci.ContentId == contentId);
  }

  /// <summary>
  /// Get all interactions for a user
  /// </summary>
  public async Task<IEnumerable<ContentInteraction>> GetUserInteractionsAsync(Guid programUserId) {
    return await context.ContentInteractions
                        .Include(ci => ci.Content)
                        .Include(ci => ci.ActivityGrades)
                        .Where(ci => ci.ProgramUserId == programUserId)
                        .OrderByDescending(ci => ci.LastAccessedAt)
                        .ToListAsync();
  }

  /// <summary>
  /// Update time spent on content
  /// </summary>
  public async Task<ContentInteraction> UpdateTimeSpentAsync(Guid interactionId, int additionalMinutes) {
    var interaction = await GetInteractionByIdAsync(interactionId);

    if (interaction.SubmittedAt.HasValue) throw new InvalidOperationException("Cannot update time spent on submitted interaction.");

    interaction.TimeSpentMinutes = (interaction.TimeSpentMinutes ?? 0) + additionalMinutes;
    interaction.LastAccessedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return interaction;
  }

  /// <summary>
  /// Get interaction by ID with proper error handling
  /// </summary>
  private async Task<ContentInteraction> GetInteractionByIdAsync(Guid interactionId) {
    var interaction = await context.ContentInteractions
                                   .Include(ci => ci.ProgramUser)
                                   .Include(ci => ci.Content)
                                   .Include(ci => ci.ActivityGrades)
                                   .FirstOrDefaultAsync(ci => ci.Id == interactionId);

    if (interaction == null) throw new InvalidOperationException($"Content interaction with ID {interactionId} not found.");

    return interaction;
  }

  /// <summary>
  /// Create a new interaction based on previous submission data
  /// This allows users to continue working after submitting
  /// </summary>
  private async Task<ContentInteraction> CreateNewInteractionFromPreviousAsync(ContentInteraction previousInteraction) {
    var newInteraction = new ContentInteraction {
      ProgramUserId = previousInteraction.ProgramUserId,
      ContentId = previousInteraction.ContentId,
      Status = ProgressStatus.InProgress,
      FirstAccessedAt = DateTime.UtcNow,
      LastAccessedAt = DateTime.UtcNow,
      CompletionPercentage = 0,
      // Initialize with previous submission data as starting point
      SubmissionData = previousInteraction.SubmissionData,
    };

    context.ContentInteractions.Add(newInteraction);
    await context.SaveChangesAsync();

    return newInteraction;
  }
}
