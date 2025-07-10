using GameGuild.Common.Authorization;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Programs.Interfaces;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Programs.GraphQL;

/// <summary>
/// GraphQL queries for ActivityGrade operations
/// Following permission inheritance: ActivityGrade permissions come from parent Program
/// </summary>
[ExtendObjectType<DbLoggerCategory.Query>]
public class ActivityGradeQueries {
  /// <summary>
  /// Get grade for a specific content interaction
  /// Requires Read permission on the parent Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
  public async Task<ActivityGrade?> GetActivityGrade(
    Guid programId,
    Guid contentInteractionId,
    [Service] IActivityGradeService activityGradeService,
    [Service] IContentInteractionService contentInteractionService
  ) {
    // Verify the content interaction belongs to the specified program
    var interactions = await contentInteractionService.GetUserInteractionsAsync(Guid.Empty);
    var interaction = interactions.FirstOrDefault(i => i.Id == contentInteractionId);

    if (interaction?.Content?.ProgramId != programId) return null;

    return await activityGradeService.GetGradeAsync(contentInteractionId);
  }

  /// <summary>
  /// Get grade by its ID
  /// Requires Read permission on the parent Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
  public async Task<ActivityGrade?> GetActivityGradeById(
    Guid programId,
    Guid gradeId,
    [Service] IActivityGradeService activityGradeService
  ) {
    var grade = await activityGradeService.GetGradeByIdAsync(gradeId);

    // Verify the grade belongs to the specified program
    if (grade?.ContentInteraction?.Content?.ProgramId != programId) return null;

    return grade;
  }

  /// <summary>
  /// Get all grades given by a specific grader
  /// Requires Read permission on the parent Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
  public async Task<IEnumerable<ActivityGrade>> GetGradesByGrader(
    Guid programId,
    Guid graderProgramUserId,
    [Service] IActivityGradeService activityGradeService
  ) {
    var grades = await activityGradeService.GetGradesByGraderAsync(graderProgramUserId);

    // Filter to only grades for this program
    return grades.Where(g => g.ContentInteraction.Content.ProgramId == programId);
  }

  /// <summary>
  /// Get all grades received by a specific student
  /// Requires Read permission on the parent Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
  public async Task<IEnumerable<ActivityGrade>> GetGradesByStudent(
    Guid programId,
    Guid programUserId,
    [Service] IActivityGradeService activityGradeService
  ) {
    var grades = await activityGradeService.GetGradesByStudentAsync(programUserId);

    // Filter to only grades for this program
    return grades.Where(g => g.ContentInteraction.Content.ProgramId == programId);
  }

  /// <summary>
  /// Get all grades for a specific content item
  /// Requires Read permission on the parent Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
  public async Task<IEnumerable<ActivityGrade>> GetGradesByContent(
    Guid programId,
    Guid contentId,
    [Service] IActivityGradeService activityGradeService,
    [Service] IProgramContentService programContentService
  ) {
    // Verify content belongs to the specified program
    var content = await programContentService.GetContentByIdAsync(contentId);

    if (content?.ProgramId != programId) return [];

    return await activityGradeService.GetGradesByContentAsync(contentId);
  }

  /// <summary>
  /// Get content interactions that need grading (submitted but not yet graded)
  /// Requires Read permission on the parent Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
  public async Task<IEnumerable<ContentInteraction>> GetPendingGrades(
    Guid programId,
    [Service] IActivityGradeService activityGradeService
  ) {
    return await activityGradeService.GetPendingGradesAsync(programId);
  }

  /// <summary>
  /// Get grade statistics for a program
  /// Requires Read permission on the parent Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
  public async Task<ActivityGradeStatistics> GetGradeStatistics(
    Guid programId,
    [Service] IActivityGradeService activityGradeService
  ) {
    var statistics = await activityGradeService.GetGradeStatisticsAsync(programId);

    return new ActivityGradeStatistics {
      TotalGrades = statistics.TotalGrades,
      AverageGrade = statistics.AverageGrade,
      MinGrade = statistics.MinGrade,
      MaxGrade = statistics.MaxGrade,
      PassingRate = statistics.PassingRate,
    };
  }
}
