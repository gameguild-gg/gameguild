using GameGuild.Common;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Programs.Interfaces;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Programs.GraphQL;

/// <summary>
/// GraphQL queries for ContentInteraction operations
/// Following permission inheritance: ContentInteraction permissions come from parent Program
/// </summary>
[ExtendObjectType<DbLoggerCategory.Query>]
public class ContentInteractionQueries {
  /// <summary>
  /// Get content interaction by ID
  /// Requires Read permission on the parent Program
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
  public async Task<ContentInteraction?> GetContentInteractionById(
    Guid programId,
    Guid interactionId,
    [Service] IContentInteractionService contentInteractionService,
    [Service] IProgramContentService programContentService
  ) {
    // Get the interaction
    var interactions = await contentInteractionService.GetUserInteractionsAsync(Guid.Empty);
    var interaction = interactions.FirstOrDefault(i => i.Id == interactionId);

    if (interaction == null) return null;

    // Verify the interaction belongs to content in the specified program
    var content = await programContentService.GetContentByIdAsync(interaction.ContentId);

    if (content == null || content.ProgramId != programId) return null;

    return interaction;
  }

  /// <summary>
  /// Get content interaction for a specific user and content
  /// Requires Read permission on the parent Program
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
  public async Task<ContentInteraction?> GetUserContentInteraction(
    Guid programId,
    Guid programUserId,
    Guid contentId,
    [Service] IContentInteractionService contentInteractionService,
    [Service] IProgramContentService programContentService
  ) {
    // Verify content belongs to the specified program
    var content = await programContentService.GetContentByIdAsync(contentId);

    if (content == null || content.ProgramId != programId) return null;

    var interaction = await contentInteractionService.GetInteractionAsync(programUserId, contentId);

    return interaction;
  }

  /// <summary>
  /// Get all content interactions for a user in a program
  /// Requires Read permission on the parent Program
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
  public async Task<IEnumerable<ContentInteraction>> GetUserContentInteractions(
    Guid programId,
    Guid programUserId,
    [Service] IContentInteractionService contentInteractionService
  ) {
    var interactions = await contentInteractionService.GetUserInteractionsAsync(programUserId);

    // Filter to only interactions for content in the specified program
    return interactions.Where(i => i.Content.ProgramId == programId);
  }

  /// <summary>
  /// Get content interactions by status for a program
  /// Requires Read permission on the parent Program
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
  public Task<IEnumerable<ContentInteraction>> GetContentInteractionsByStatus(
    Guid programId,
    ProgressStatus status,
    [Service] IContentInteractionService contentInteractionService
  ) {
    // This would require extending the service to support filtering by status
    // For now, we'll get all interactions and filter in memory
    // In a production system, this should be moved to the service layer for better performance
    var allInteractions = new List<ContentInteraction>();

    // Note: This is a simplified implementation. In practice, you'd want a service method
    // that can efficiently query interactions by program and status
    return Task.FromResult(allInteractions.Where(i => i.Content.ProgramId == programId && i.Status == status));
  }

  /// <summary>
  /// Get content interaction statistics for a program
  /// Requires Read permission on the parent Program
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
  public Task<ContentInteractionStats> GetContentInteractionStats(
    Guid programId,
    [Service] IContentInteractionService contentInteractionService
  ) {
    // This would require extending the service to support statistics
    // For now, return a placeholder implementation
    return Task.FromResult(new ContentInteractionStats {
      ProgramId = programId,
      TotalInteractions = 0,
      CompletedInteractions = 0,
      SubmittedInteractions = 0,
      InProgressInteractions = 0,
      AverageCompletionPercentage = 0,
      AverageTimeSpentMinutes = 0,
    });
  }
}
