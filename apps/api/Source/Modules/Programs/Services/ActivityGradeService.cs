using GameGuild.Database;
using GameGuild.Modules.Programs.Interfaces;
using GameGuild.Modules.Programs.Models;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Programs.Services;

/// <summary>
/// Service implementation for ActivityGrade management with full permission inheritance
/// Handles grading operations following permission chain: ActivityGrade → ContentInteraction → ProgramContent → Program
/// </summary>
public class ActivityGradeService(ApplicationDbContext context) : IActivityGradeService {
  /// <summary>
  /// Grade a content interaction - creates or updates existing grade
  /// </summary>
  public async Task<ActivityGrade> GradeActivityAsync(Guid contentInteractionId, Guid graderProgramUserId, decimal grade, string? feedback = null, string? gradingDetails = null) {
    // Validate the content interaction exists and get the program context
    var contentInteraction = await context.ContentInteractions
                                          .Include(ci => ci.Content)
                                          .ThenInclude(c => c.Program)
                                          .Include(ci => ci.ProgramUser)
                                          .FirstOrDefaultAsync(ci => ci.Id == contentInteractionId);

    if (contentInteraction == null) throw new ArgumentException("Content interaction not found", nameof(contentInteractionId));

    // Validate the grader is part of the same program
    var graderProgramUser = await context.ProgramUsers
                                         .FirstOrDefaultAsync(pu => pu.Id == graderProgramUserId && pu.ProgramId == contentInteraction.Content.ProgramId);

    if (graderProgramUser == null) throw new ArgumentException("Grader is not a member of this program", nameof(graderProgramUserId));

    // Check if a grade already exists for this interaction
    var existingGrade = await context.ActivityGrades
                                     .FirstOrDefaultAsync(ag => ag.ContentInteractionId == contentInteractionId);

    if (existingGrade != null) {
      // Update existing grade
      existingGrade.Grade = grade;
      existingGrade.Feedback = feedback;
      existingGrade.GradingDetails = gradingDetails;
      existingGrade.GraderProgramUserId = graderProgramUserId;
      existingGrade.GradedAt = DateTime.UtcNow;
      existingGrade.Touch();

      await context.SaveChangesAsync();

      return existingGrade;
    }
    else {
      // Create new grade
      var newGrade = new ActivityGrade {
        ContentInteractionId = contentInteractionId,
        GraderProgramUserId = graderProgramUserId,
        Grade = grade,
        Feedback = feedback,
        GradingDetails = gradingDetails ?? "{}",
        GradedAt = DateTime.UtcNow,
      };

      context.ActivityGrades.Add(newGrade);
      await context.SaveChangesAsync();

      return newGrade;
    }
  }

  /// <summary>
  /// Get grade for a specific content interaction
  /// </summary>
  public async Task<ActivityGrade?> GetGradeAsync(Guid contentInteractionId) {
    return await context.ActivityGrades
                        .Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.Content)
                        .Include(ag => ag.GraderProgramUser)
                        .ThenInclude(gpu => gpu.User)
                        .FirstOrDefaultAsync(ag => ag.ContentInteractionId == contentInteractionId);
  }

  /// <summary>
  /// Get grade by its ID
  /// </summary>
  public async Task<ActivityGrade?> GetGradeByIdAsync(Guid gradeId) {
    return await context.ActivityGrades
                        .Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.Content)
                        .ThenInclude(c => c.Program)
                        .Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.ProgramUser)
                        .ThenInclude(pu => pu.User)
                        .Include(ag => ag.GraderProgramUser)
                        .ThenInclude(gpu => gpu.User)
                        .FirstOrDefaultAsync(ag => ag.Id == gradeId);
  }

  /// <summary>
  /// Get all grades given by a specific grader
  /// </summary>
  public async Task<IEnumerable<ActivityGrade>> GetGradesByGraderAsync(Guid graderProgramUserId) {
    return await context.ActivityGrades
                        .Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.Content)
                        .Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.ProgramUser)
                        .ThenInclude(pu => pu.User)
                        .Where(ag => ag.GraderProgramUserId == graderProgramUserId)
                        .OrderByDescending(ag => ag.GradedAt)
                        .ToListAsync();
  }

  /// <summary>
  /// Get all grades received by a specific program user
  /// </summary>
  public async Task<IEnumerable<ActivityGrade>> GetGradesByStudentAsync(Guid programUserId) {
    return await context.ActivityGrades
                        .Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.Content)
                        .Include(ag => ag.GraderProgramUser)
                        .ThenInclude(gpu => gpu.User)
                        .Where(ag => ag.ContentInteraction.ProgramUserId == programUserId)
                        .OrderByDescending(ag => ag.GradedAt)
                        .ToListAsync();
  }

  /// <summary>
  /// Update an existing grade
  /// </summary>
  public async Task<ActivityGrade?> UpdateGradeAsync(Guid gradeId, decimal? newGrade = null, string? newFeedback = null, string? newGradingDetails = null) {
    var grade = await context.ActivityGrades.FirstOrDefaultAsync(ag => ag.Id == gradeId);

    if (grade == null) return null;

    if (newGrade.HasValue) grade.Grade = newGrade.Value;
    if (newFeedback != null) grade.Feedback = newFeedback;
    if (newGradingDetails != null) grade.GradingDetails = newGradingDetails;
    grade.GradedAt = DateTime.UtcNow;
    grade.Touch();

    await context.SaveChangesAsync();

    return grade;
  }

  /// <summary>
  /// Delete a grade
  /// </summary>
  public async Task<bool> DeleteGradeAsync(Guid gradeId) {
    var grade = await context.ActivityGrades.FirstOrDefaultAsync(ag => ag.Id == gradeId);

    if (grade == null) return false;

    context.ActivityGrades.Remove(grade);
    await context.SaveChangesAsync();

    return true;
  }

  /// <summary>
  /// Get all pending grades for a program (content interactions that need grading)
  /// </summary>
  public async Task<IEnumerable<ContentInteraction>> GetPendingGradesAsync(Guid programId) {
    return await context.ContentInteractions
                        .Include(ci => ci.Content)
                        .Include(ci => ci.ProgramUser)
                        .ThenInclude(pu => pu.User)
                        .Where(ci => ci.Content.ProgramId == programId && ci.SubmittedAt.HasValue && !context.ActivityGrades.Any(ag => ag.ContentInteractionId == ci.Id))
                        .OrderBy(ci => ci.SubmittedAt)
                        .ToListAsync();
  }

  /// <summary>
  /// Get grade statistics for a program
  /// </summary>
  public async Task<GradeStatistics> GetGradeStatisticsAsync(Guid programId) {
    var grades = await context.ActivityGrades
                              .Include(ag => ag.ContentInteraction)
                              .ThenInclude(ci => ci.Content)
                              .Where(ag => ag.ContentInteraction.Content.ProgramId == programId)
                              .Select(ag => ag.Grade)
                              .ToListAsync();

    if (!grades.Any())
      return new GradeStatistics {
        TotalGrades = 0,
        AverageGrade = 0,
        MinGrade = 0,
        MaxGrade = 0,
        PassingRate = 0,
      };

    return new GradeStatistics {
      TotalGrades = grades.Count,
      AverageGrade = grades.Average(),
      MinGrade = grades.Min(),
      MaxGrade = grades.Max(),
      PassingRate = grades.Count(g => g >= 60) / (decimal)grades.Count * 100, // Assuming 60 is passing
    };
  }

  /// <summary>
  /// Get grades for a specific content item across all students
  /// </summary>
  public async Task<IEnumerable<ActivityGrade>> GetGradesByContentAsync(Guid contentId) {
    return await context.ActivityGrades
                        .Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.ProgramUser)
                        .ThenInclude(pu => pu.User)
                        .Include(ag => ag.GraderProgramUser)
                        .ThenInclude(gpu => gpu.User)
                        .Where(ag => ag.ContentInteraction.ContentId == contentId)
                        .OrderByDescending(ag => ag.GradedAt)
                        .ToListAsync();
  }
}
