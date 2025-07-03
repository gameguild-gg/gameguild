using GameGuild.Common.GraphQL.Authorization;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Programs.Interfaces;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Users.GraphQL;


namespace GameGuild.Modules.Programs.GraphQL;

/// <summary>
/// GraphQL mutations for ActivityGrade operations
/// Following permission inheritance: ActivityGrade permissions come from parent Program
/// </summary>
[ExtendObjectType<Mutation>]
public class ActivityGradeMutations {
  /// <summary>
  /// Grade a content interaction (create or update existing grade)
  /// Requires Edit permission on the parent Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit, "programId")]
  public async Task<ActivityGradeResult> GradeActivity(
    Guid programId,
    CreateActivityGradeInput input,
    [Service] IActivityGradeService activityGradeService,
    [Service] IContentInteractionService contentInteractionService
  ) {
    try {
      // Verify the content interaction belongs to the specified program
      var interactions = await contentInteractionService.GetUserInteractionsAsync(Guid.Empty);
      var interaction = interactions.FirstOrDefault(i => i.Id == input.ContentInteractionId);

      if (interaction?.Content?.ProgramId != programId) { return new ActivityGradeResult { Success = false, ErrorMessage = "Content interaction does not belong to the specified program.", Grade = null }; }

      var grade = await activityGradeService.GradeActivityAsync(
                    input.ContentInteractionId,
                    input.GraderProgramUserId,
                    input.Grade,
                    input.Feedback,
                    input.GradingDetails
                  );

      return new ActivityGradeResult { Success = true, ErrorMessage = null, Grade = grade };
    }
    catch (ArgumentException ex) { return new ActivityGradeResult { Success = false, ErrorMessage = ex.Message, Grade = null }; }
    catch (InvalidOperationException ex) { return new ActivityGradeResult { Success = false, ErrorMessage = ex.Message, Grade = null }; }
  }

  /// <summary>
  /// Update an existing grade
  /// Requires Edit permission on the parent Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit, "programId")]
  public async Task<ActivityGradeResult> UpdateActivityGrade(
    Guid programId,
    UpdateActivityGradeInput input,
    [Service] IActivityGradeService activityGradeService
  ) {
    try {
      // Verify the grade belongs to the specified program
      var existingGrade = await activityGradeService.GetGradeByIdAsync(input.GradeId);

      if (existingGrade?.ContentInteraction?.Content?.ProgramId != programId) { return new ActivityGradeResult { Success = false, ErrorMessage = "Grade does not belong to the specified program.", Grade = null }; }

      var updatedGrade = await activityGradeService.UpdateGradeAsync(
                           input.GradeId,
                           input.Grade,
                           input.Feedback,
                           input.GradingDetails
                         );

      if (updatedGrade == null) { return new ActivityGradeResult { Success = false, ErrorMessage = "Grade not found.", Grade = null }; }

      return new ActivityGradeResult { Success = true, ErrorMessage = null, Grade = updatedGrade };
    }
    catch (Exception ex) { return new ActivityGradeResult { Success = false, ErrorMessage = ex.Message, Grade = null }; }
  }

  /// <summary>
  /// Delete a grade
  /// Requires Delete permission on the parent Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Delete, "programId")]
  public async Task<ActivityGradeResult> DeleteActivityGrade(
    Guid programId,
    Guid gradeId,
    [Service] IActivityGradeService activityGradeService
  ) {
    try {
      // Verify the grade belongs to the specified program
      var existingGrade = await activityGradeService.GetGradeByIdAsync(gradeId);

      if (existingGrade?.ContentInteraction?.Content?.ProgramId != programId) { return new ActivityGradeResult { Success = false, ErrorMessage = "Grade does not belong to the specified program.", Grade = null }; }

      var deleted = await activityGradeService.DeleteGradeAsync(gradeId);

      if (!deleted) { return new ActivityGradeResult { Success = false, ErrorMessage = "Grade not found or could not be deleted.", Grade = null }; }

      return new ActivityGradeResult { Success = true, ErrorMessage = null, Grade = null };
    }
    catch (Exception ex) { return new ActivityGradeResult { Success = false, ErrorMessage = ex.Message, Grade = null }; }
  }
}
