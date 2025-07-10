using GameGuild.Common.Authorization;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Programs.Interfaces;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Programs.GraphQL;

/// <summary>
/// GraphQL mutations for ContentInteraction operations
/// Following permission inheritance: ContentInteraction permissions come from parent Program
/// Implements immutable submission pattern: once submitted, interactions cannot be changed
/// </summary>
[ExtendObjectType<Mutation>]
public class ContentInteractionMutations {
  /// <summary>
  /// Start or resume content interaction
  /// Requires Read permission on the parent Program
  /// If the interaction was previously submitted, creates a new interaction based on the previous data
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
  public async Task<ContentInteractionResult> StartContentInteraction(
    Guid programId,
    StartContentInput input,
    [Service] IContentInteractionService contentInteractionService,
    [Service] IProgramContentService programContentService
  ) {
    try {
      // Verify content belongs to the specified program
      var content = await programContentService.GetContentByIdAsync(input.ContentId);

      if (content == null || content.ProgramId != programId) return new ContentInteractionResult { Success = false, ErrorMessage = "Content does not belong to the specified program.", Interaction = null };

      var interaction = await contentInteractionService.StartContentAsync(
                          input.ProgramUserId,
                          input.ContentId
                        );

      return new ContentInteractionResult { Success = true, ErrorMessage = null, Interaction = interaction };
    }
    catch (Exception ex) { return new ContentInteractionResult { Success = false, ErrorMessage = ex.Message, Interaction = null }; }
  }

  /// <summary>
  /// Update progress for a content interaction
  /// Requires Edit permission on the parent Program
  /// Cannot update submitted interactions
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit, "programId")]
  public async Task<ContentInteractionResult> UpdateContentProgress(
    Guid programId,
    UpdateProgressInput input,
    [Service] IContentInteractionService contentInteractionService
  ) {
    try {
      var interaction = await contentInteractionService.UpdateProgressAsync(
                          input.InteractionId,
                          input.CompletionPercentage
                        );

      // Verify the interaction belongs to the specified program
      if (interaction.Content.ProgramId != programId) return new ContentInteractionResult { Success = false, ErrorMessage = "Interaction does not belong to the specified program.", Interaction = null };

      return new ContentInteractionResult { Success = true, ErrorMessage = null, Interaction = interaction };
    }
    catch (InvalidOperationException ex) { return new ContentInteractionResult { Success = false, ErrorMessage = ex.Message, Interaction = null }; }
    catch (Exception ex) { return new ContentInteractionResult { Success = false, ErrorMessage = "An unexpected error occurred while updating progress.", Interaction = null }; }
  }

  /// <summary>
  /// Submit content interaction (makes it immutable)
  /// Requires Edit permission on the parent Program
  /// Once submitted, the interaction cannot be modified
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit, "programId")]
  public async Task<ContentInteractionResult> SubmitContentInteraction(
    Guid programId,
    SubmitContentInput input,
    [Service] IContentInteractionService contentInteractionService
  ) {
    try {
      var interaction = await contentInteractionService.SubmitContentAsync(
                          input.InteractionId,
                          input.SubmissionData
                        );

      // Verify the interaction belongs to the specified program
      if (interaction.Content.ProgramId != programId) return new ContentInteractionResult { Success = false, ErrorMessage = "Interaction does not belong to the specified program.", Interaction = null };

      return new ContentInteractionResult { Success = true, ErrorMessage = null, Interaction = interaction };
    }
    catch (InvalidOperationException ex) { return new ContentInteractionResult { Success = false, ErrorMessage = ex.Message, Interaction = null }; }
    catch (Exception ex) { return new ContentInteractionResult { Success = false, ErrorMessage = "An unexpected error occurred while submitting the interaction.", Interaction = null }; }
  }

  /// <summary>
  /// Mark content as completed
  /// Requires Edit permission on the parent Program
  /// Cannot complete submitted interactions
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit, "programId")]
  public async Task<ContentInteractionResult> CompleteContentInteraction(
    Guid programId,
    CompleteContentInput input,
    [Service] IContentInteractionService contentInteractionService
  ) {
    try {
      var interaction = await contentInteractionService.CompleteContentAsync(input.InteractionId);

      // Verify the interaction belongs to the specified program
      if (interaction.Content.ProgramId != programId) return new ContentInteractionResult { Success = false, ErrorMessage = "Interaction does not belong to the specified program.", Interaction = null };

      return new ContentInteractionResult { Success = true, ErrorMessage = null, Interaction = interaction };
    }
    catch (InvalidOperationException ex) { return new ContentInteractionResult { Success = false, ErrorMessage = ex.Message, Interaction = null }; }
    catch (Exception ex) { return new ContentInteractionResult { Success = false, ErrorMessage = "An unexpected error occurred while completing the interaction.", Interaction = null }; }
  }

  /// <summary>
  /// Update time spent on content
  /// Requires Edit permission on the parent Program
  /// Cannot update time on submitted interactions
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit, "programId")]
  public async Task<ContentInteractionResult> UpdateTimeSpent(
    Guid programId,
    UpdateTimeSpentInput input,
    [Service] IContentInteractionService contentInteractionService
  ) {
    try {
      var interaction = await contentInteractionService.UpdateTimeSpentAsync(
                          input.InteractionId,
                          input.AdditionalMinutes
                        );

      // Verify the interaction belongs to the specified program
      if (interaction.Content.ProgramId != programId) return new ContentInteractionResult { Success = false, ErrorMessage = "Interaction does not belong to the specified program.", Interaction = null };

      return new ContentInteractionResult { Success = true, ErrorMessage = null, Interaction = interaction };
    }
    catch (InvalidOperationException ex) { return new ContentInteractionResult { Success = false, ErrorMessage = ex.Message, Interaction = null }; }
    catch (Exception ex) { return new ContentInteractionResult { Success = false, ErrorMessage = "An unexpected error occurred while updating time spent.", Interaction = null }; }
  }
}

/// <summary>
/// Result type for ContentInteraction mutations with proper error handling
/// </summary>
public class ContentInteractionResult {
  public bool Success { get; set; }

  public string? ErrorMessage { get; set; }

  public ContentInteraction? Interaction { get; set; }
}
