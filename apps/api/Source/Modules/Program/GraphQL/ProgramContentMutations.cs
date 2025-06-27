using GameGuild.Common.GraphQL.Authorization;
using GameGuild.Common.Entities;
using GameGuild.Modules.Program.Interfaces;
using GameGuild.Modules.Program.Models;
using HotChocolate;
using HotChocolate.Types;
using ProgramContentTypeEnum = GameGuild.Common.Enums.ProgramContentType;
using VisibilityEnum = GameGuild.Common.Enums.Visibility;
using GradingMethodEnum = GameGuild.Common.Enums.GradingMethod;


namespace GameGuild.Modules.Program.GraphQL;

[ExtendObjectType<GameGuild.Modules.User.GraphQL.Mutation>]
public class ProgramContentMutations {
  /// <summary>
  /// Create new program content (Resource Level: Create permission required for the parent Program)
  /// Layer 3: Resource Level - User needs Create permission on the specific Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Create, "programId")]
  public async Task<ProgramContent> CreateContentAsync(
    [Service] IProgramContentService contentService,
    Guid programId,
    string title,
    ProgramContentTypeEnum type,
    string body,
    string description,
    int estimatedMinutes,
    VisibilityEnum visibility = VisibilityEnum.Published,
    Guid? parentId = null,
    int? sortOrder = null,
    bool isRequired = false,
    GradingMethodEnum? gradingMethod = null,
    int maxPoints = 0
  ) {
    var content = new ProgramContent {
      ProgramId = programId,
      Title = title,
      Type = type,
      Body = body,
      Description = description,
      EstimatedMinutes = estimatedMinutes,
      Visibility = visibility,
      ParentId = parentId,
      SortOrder = sortOrder ?? 0,
      IsRequired = isRequired,
      GradingMethod = gradingMethod,
      MaxPoints = maxPoints
    };

    return await contentService.CreateContentAsync(content);
  }

  /// <summary>
  /// Update specific program content (Resource Level: Edit permission required for the parent Program)
  /// Layer 3: Resource Level - User needs Edit permission on the parent Program
  /// Note: The programId will be resolved from the content's ProgramId property
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit, "programId")]
  public async Task<ProgramContent> UpdateContentAsync(
    [Service] IProgramContentService contentService,
    Guid contentId,
    string? title = null,
    ProgramContentTypeEnum? type = null,
    string? body = null,
    string? description = null,
    int? estimatedMinutes = null,
    VisibilityEnum? visibility = null,
    int? sortOrder = null,
    bool? isRequired = null,
    GradingMethodEnum? gradingMethod = null,
    int? maxPoints = null
  ) {
    var existingContent = await contentService.GetContentByIdAsync(contentId);

    if (existingContent == null) { throw new ArgumentException($"Content with ID {contentId} not found"); }

    // Update only provided fields
    if (title != null) existingContent.Title = title;
    if (type.HasValue) existingContent.Type = type.Value;
    if (body != null) existingContent.Body = body;
    if (description != null) existingContent.Description = description;
    if (estimatedMinutes.HasValue) existingContent.EstimatedMinutes = estimatedMinutes.Value;
    if (visibility.HasValue) existingContent.Visibility = visibility.Value;
    if (sortOrder.HasValue) existingContent.SortOrder = sortOrder.Value;
    if (isRequired.HasValue) existingContent.IsRequired = isRequired.Value;
    if (gradingMethod.HasValue) existingContent.GradingMethod = gradingMethod;
    if (maxPoints.HasValue) existingContent.MaxPoints = maxPoints.Value;

    return await contentService.UpdateContentAsync(existingContent);
  }

  /// <summary>
  /// Delete specific program content (Resource Level: Delete permission required for the parent Program)
  /// Layer 3: Resource Level - User needs Delete permission on the parent Program
  /// Note: The programId will be resolved from the content's ProgramId property
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Delete, "programId")]
  public async Task<bool> DeleteContentAsync([Service] IProgramContentService contentService, Guid contentId) { return await contentService.DeleteContentAsync(contentId); }

  /// <summary>
  /// Move specific program content (Resource Level: Edit permission required for the parent Program)
  /// Layer 3: Resource Level - User needs Edit permission on the parent Program
  /// Note: The programId will be resolved from the content's ProgramId property
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit, "programId")]
  public async Task<bool> MoveContentAsync(
    [Service] IProgramContentService contentService, Guid contentId,
    Guid? newParentId, int newSortOrder = 0
  ) {
    return await contentService.MoveContentAsync(contentId, newParentId, newSortOrder);
  }

  /// <summary>
  /// Reorder program content (Resource Level: Edit permission required for the parent Program)
  /// Layer 3: Resource Level - User needs Edit permission on the specific Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit, "programId")]
  public async Task<bool> ReorderContentAsync(
    [Service] IProgramContentService contentService, Guid programId,
    List<Guid> contentIds, List<int> sortOrders
  ) {
    if (contentIds.Count != sortOrders.Count) { throw new ArgumentException("ContentIds and SortOrders must have the same length"); }

    var reorderList = contentIds.Zip(sortOrders, (id, order) => (id, order)).ToList();

    return await contentService.ReorderContentAsync(programId, reorderList);
  }
}
