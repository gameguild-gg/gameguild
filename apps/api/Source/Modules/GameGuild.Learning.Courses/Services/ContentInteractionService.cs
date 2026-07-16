using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Learning.Courses;

/// <summary>
///   Service for managing content interactions following the permission inheritance pattern ContentInteraction inherits permissions from Program -> ProgramContent -> ContentInteraction Once submitted, interactions become immutable but
///   users can create new interactions
/// </summary>
public class ContentInteractionService(
  IApplicationDbContext context,
  IRequestContextAccessor requestContextAccessor) : IContentInteractionService {
  /// <summary> Start a new content interaction (or resume existing one if not submitted) </summary>
  public async Task<ContentInteraction> StartContentAsync(Guid programUserId, Guid contentId) {
    var currentUserId = requestContextAccessor.CurrentUserId;
    if (!currentUserId.HasValue)
      throw new RequestValidationException("Active course enrollment was not found.");

    var programUser = await context.Set<ProgramUser>()
      .AsNoTracking()
      .FirstOrDefaultAsync(item =>
        item.Id == programUserId &&
        item.UserId == currentUserId.Value &&
        item.DeletedAt == null &&
        item.IsActive)
      .ConfigureAwait(false);
    if (programUser == null) throw new RequestValidationException("Active course enrollment was not found.");

    var content = await context.Set<ProgramContent>()
      .FirstOrDefaultAsync(item => item.Id == contentId && item.ProgramId == programUser.ProgramId && item.DeletedAt == null)
      .ConfigureAwait(false);
    if (content is null) throw new InvalidOperationException("Content does not belong to the enrolled course.");

    // Check if there's already an interaction for this user/content
    var existingInteraction = await context.Set<ContentInteraction>()
      .Where(ci => ci.ProgramUserId == programUserId && ci.ContentId == contentId)
      .OrderBy(ci => ci.SubmittedAt.HasValue)
      .ThenByDescending(ci => ci.CreatedAt)
      .FirstOrDefaultAsync()
      .ConfigureAwait(false);

    if (existingInteraction != null) {
      existingInteraction.UserId = programUser.UserId;

      // If already submitted, create a new interaction based on the last one
      if (existingInteraction.SubmittedAt.HasValue) {
        if (content.Type == ProgramContentType.Survey && !LearningActivityContract.AllowsMultipleResponses(content))
          throw new InvalidOperationException("This survey accepts only one response.");
        return await CreateNewInteractionFromPreviousAsync(existingInteraction).ConfigureAwait(false);
      }

      // Otherwise, resume the existing interaction
      existingInteraction.FirstAccessedAt ??= SystemClock.UtcNow;
      existingInteraction.LastAccessedAt = SystemClock.UtcNow;
      if (!existingInteraction.IsCompleted)
        existingInteraction.Status = ProgressStatus.InProgress;

      await context.SaveChangesAsync().ConfigureAwait(false);

      return existingInteraction;
    }

    // Create new interaction
    var newInteraction = new ContentInteraction { ProgramUserId = programUserId, UserId = programUser.UserId, ContentId = contentId, Status = ProgressStatus.InProgress, FirstAccessedAt = SystemClock.UtcNow, LastAccessedAt = SystemClock.UtcNow, CompletionPercentage = 0 };

    return await SaveNewActiveAttemptAsync(newInteraction).ConfigureAwait(false);
  }

  /// <summary> Update progress for an interaction (only if not submitted) </summary>
  public async Task<ContentInteraction> UpdateProgressAsync(Guid interactionId, decimal completionPercentage) {
    var interaction = await GetInteractionByIdAsync(interactionId).ConfigureAwait(false);

    if (interaction.SubmittedAt.HasValue) throw new InvalidOperationException("Cannot update progress on submitted interaction. Create a new interaction to continue work.");

    interaction.UpdateProgress(completionPercentage);
    if (!interaction.IsCompleted && interaction.Status == ProgressStatus.NotStarted)
      interaction.Status = ProgressStatus.InProgress;

    await context.SaveChangesAsync().ConfigureAwait(false);

    return interaction;
  }

  /// <summary> Submit content interaction (makes it immutable) </summary>
  public async Task<ContentInteraction> SubmitContentAsync(Guid interactionId, string submissionData) {
    var interaction = await GetInteractionByIdAsync(interactionId).ConfigureAwait(false);

    if (interaction.SubmittedAt.HasValue) throw new InvalidOperationException("Interaction has already been submitted and cannot be changed.");

    if (LearningActivityContract.IsActivityType(interaction.Content.Type))
      ActivityResponseContract.Parse(interaction.Content.Type, submissionData, interaction.Content.GetActivitySettings());

    interaction.SubmissionData = submissionData;
    interaction.SubmittedAt = SystemClock.UtcNow;
    interaction.Complete();

    await context.SaveChangesAsync().ConfigureAwait(false);

    return interaction;
  }

  /// <summary> Mark content as completed </summary>
  public async Task<ContentInteraction> CompleteContentAsync(Guid interactionId) {
    var interaction = await GetInteractionByIdAsync(interactionId).ConfigureAwait(false);

    if (interaction.SubmittedAt.HasValue) throw new InvalidOperationException("Cannot modify submitted interaction. Create a new interaction to continue work.");

    interaction.Complete();

    await context.SaveChangesAsync().ConfigureAwait(false);

    return interaction;
  }

  /// <summary> Get interaction for a specific user and content </summary>
  public async Task<ContentInteraction?> GetInteractionAsync(Guid programUserId, Guid contentId) {
    var currentUserId = requestContextAccessor.CurrentUserId;
    if (!currentUserId.HasValue) return null;

    return await context.Set<ContentInteraction>().Include(ci => ci.ProgramUser).Include(ci => ci.Content).Include(ci => ci.ActivityGrades)
      .OrderBy(ci => ci.SubmittedAt.HasValue)
      .ThenByDescending(ci => ci.CreatedAt)
      .FirstOrDefaultAsync(ci =>
        ci.ProgramUserId == programUserId &&
        ci.ContentId == contentId &&
        ci.UserId == currentUserId.Value)
      .ConfigureAwait(false);
  }

  /// <summary> Get all interactions for a user </summary>
  public async Task<IEnumerable<ContentInteraction>> GetUserInteractionsAsync(Guid programUserId) {
    var currentUserId = requestContextAccessor.CurrentUserId;
    if (!currentUserId.HasValue) return [];

    return await context.Set<ContentInteraction>().Include(ci => ci.Content).Include(ci => ci.ActivityGrades)
      .Where(ci => ci.ProgramUserId == programUserId && ci.UserId == currentUserId.Value)
      .OrderByDescending(ci => ci.LastAccessedAt)
      .ToListAsync()
      .ConfigureAwait(false);
  }

  /// <summary>Gets survey result projections without exposing learner or enrollment identity.</summary>
  public async Task<IEnumerable<SurveyResponseResultDto>> GetSurveyResponsesAsync(Guid contentId) {
    var content = await context.Set<ProgramContent>()
      .FirstOrDefaultAsync(item => item.Id == contentId && item.DeletedAt == null)
      .ConfigureAwait(false);
    if (content is null || content.Type != ProgramContentType.Survey)
      throw new InvalidOperationException("Content is not a survey.");

    var interactions = await context.Set<ContentInteraction>()
      .Where(item => item.ContentId == contentId && item.SubmittedAt != null && item.DeletedAt == null)
      .OrderBy(item => item.SubmittedAt)
      .ToListAsync()
      .ConfigureAwait(false);

    return interactions.Select(SurveyResponseResultDto.FromInteraction).ToList();
  }

  /// <summary> Update time spent on content </summary>
  public async Task<ContentInteraction> UpdateTimeSpentAsync(Guid interactionId, int additionalMinutes) {
    var interaction = await GetInteractionByIdAsync(interactionId).ConfigureAwait(false);

    if (interaction.SubmittedAt.HasValue) throw new InvalidOperationException("Cannot update time spent on submitted interaction.");

    interaction.AddTimeSpent(additionalMinutes);

    await context.SaveChangesAsync().ConfigureAwait(false);

    return interaction;
  }

  /// <summary> Get interaction by ID with proper error handling </summary>
  private async Task<ContentInteraction> GetInteractionByIdAsync(Guid interactionId) {
    var currentUserId = requestContextAccessor.CurrentUserId;
    if (!currentUserId.HasValue)
      throw new RequestValidationException("Content interaction was not found.");

    var interaction = await context.Set<ContentInteraction>().Include(ci => ci.ProgramUser).Include(ci => ci.Content).Include(ci => ci.ActivityGrades)
      .FirstOrDefaultAsync(ci => ci.Id == interactionId && ci.UserId == currentUserId.Value)
      .ConfigureAwait(false);

    if (interaction == null) throw new RequestValidationException("Content interaction was not found.");

    return interaction;
  }

  /// <summary> Create a new interaction based on previous submission data This allows users to continue working after submitting </summary>
  private async Task<ContentInteraction> CreateNewInteractionFromPreviousAsync(ContentInteraction previousInteraction) {
    var newInteraction = new ContentInteraction {
      ProgramUserId = previousInteraction.ProgramUserId,
      UserId = previousInteraction.UserId,
      ContentId = previousInteraction.ContentId,
      Status = ProgressStatus.InProgress,
      FirstAccessedAt = SystemClock.UtcNow,
      LastAccessedAt = SystemClock.UtcNow,
      CompletionPercentage = 0,
      // Initialize with previous submission data as starting point
      SubmissionData = previousInteraction.SubmissionData,
    };

    return await SaveNewActiveAttemptAsync(newInteraction).ConfigureAwait(false);
  }

  private async Task<ContentInteraction> SaveNewActiveAttemptAsync(ContentInteraction newInteraction) {
    context.Set<ContentInteraction>().Add(newInteraction);
    try {
      await context.SaveChangesAsync().ConfigureAwait(false);
      return newInteraction;
    }
    catch (DbUpdateException) {
      context.Set<ContentInteraction>().Remove(newInteraction);
      var winningInteraction = await context.Set<ContentInteraction>()
        .Where(ci =>
          ci.ProgramUserId == newInteraction.ProgramUserId &&
          ci.UserId == newInteraction.UserId &&
          ci.ContentId == newInteraction.ContentId &&
          ci.SubmittedAt == null &&
          ci.DeletedAt == null)
        .OrderByDescending(ci => ci.CreatedAt)
        .FirstOrDefaultAsync()
        .ConfigureAwait(false);
      if (winningInteraction is not null) return winningInteraction;

      throw;
    }
  }

}
