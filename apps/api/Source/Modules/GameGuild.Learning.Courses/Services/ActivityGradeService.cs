

using Microsoft.EntityFrameworkCore;


namespace GameGuild.Learning.Courses;

/// <summary> Service implementation for ActivityGrade management with full permission inheritance Handles grading operations following permission chain: ActivityGrade → ContentInteraction → ProgramContent → Program </summary>
public class ActivityGradeService(IApplicationDbContext context) : IActivityGradeService {
  /// <summary> Grade a content interaction - creates or updates existing grade </summary>
  public async Task<ActivityGrade> GradeActivityAsync(Guid contentInteractionId, Guid graderProgramUserId, decimal grade, string? feedback = null, string? gradingDetails = null) {
    // Validate the content interaction exists and get the program context
    var contentInteraction = await context.Set<ContentInteraction>().Include(ci => ci.Content).ThenInclude(c => c.Program).Include(ci => ci.ProgramUser).FirstOrDefaultAsync(ci => ci.Id == contentInteractionId);

    if (contentInteraction == null) throw new ArgumentException("Content interaction not found", nameof(contentInteractionId));
    if (contentInteraction.Content.Type == ProgramContentType.Survey)
      throw new InvalidOperationException("Surveys cannot be graded.");

    // Validate the grader is part of the same program
    var graderProgramUser = await context.Set<ProgramUser>().FirstOrDefaultAsync(pu => pu.Id == graderProgramUserId && pu.ProgramId == contentInteraction.Content.ProgramId);

    if (graderProgramUser == null) throw new ArgumentException("Grader is not a member of this program", nameof(graderProgramUserId));

    // Check if a grade already exists for this interaction
    var existingGrade = await context.Set<ActivityGrade>().FirstOrDefaultAsync(ag => ag.ContentInteractionId == contentInteractionId);

    if (existingGrade != null) {
      // Update existing grade
      existingGrade.Grade = grade;
      existingGrade.Feedback = feedback;
      existingGrade.GradingDetails = gradingDetails;
      existingGrade.GraderProgramUserId = graderProgramUserId;
      existingGrade.GradedAt = SystemClock.UtcNow;
      existingGrade.Touch();

      await context.SaveChangesAsync().ConfigureAwait(false);

      return existingGrade;
    }

    // Create new grade
    var newGrade = new ActivityGrade { ContentInteractionId = contentInteractionId, GraderProgramUserId = graderProgramUserId, Grade = grade, Feedback = feedback, GradingDetails = gradingDetails ?? "{}", GradedAt = SystemClock.UtcNow };

    context.Set<ActivityGrade>().Add(newGrade);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return newGrade;
  }

  /// <summary> Get grade for a specific content interaction </summary>
  public async Task<ActivityGrade?> GetGradeAsync(Guid contentInteractionId) {
    return await context.Set<ActivityGrade>().Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.Content)
                        .Include(ag => ag.GraderProgramUser)
                        .ThenInclude(gpu => gpu!.User)
                        .FirstOrDefaultAsync(ag => ag.ContentInteractionId == contentInteractionId && ag.ContentInteraction.Content.Type != ProgramContentType.Survey);
  }

  /// <summary> Get grade by its ID </summary>
  public async Task<ActivityGrade?> GetGradeByIdAsync(Guid gradeId) {
    return await context.Set<ActivityGrade>().Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.Content)
                        .ThenInclude(c => c.Program)
                        .Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.ProgramUser)
                        .ThenInclude(pu => pu!.User)
                        .Include(ag => ag.GraderProgramUser)
                        .ThenInclude(gpu => gpu!.User)
                        .FirstOrDefaultAsync(ag => ag.Id == gradeId && ag.ContentInteraction.Content.Type != ProgramContentType.Survey);
  }

  /// <summary> Get all grades given by a specific grader </summary>
  public async Task<IEnumerable<ActivityGrade>> GetGradesByGraderAsync(Guid graderProgramUserId) {
    return await context.Set<ActivityGrade>().Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.Content)
                        .Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.ProgramUser)
                        .ThenInclude(pu => pu!.User)
                        .Where(ag => ag.GraderProgramUserId == graderProgramUserId && ag.ContentInteraction.Content.Type != ProgramContentType.Survey)
                        .OrderByDescending(ag => ag.GradedAt)
                        .ToListAsync();
  }

  /// <summary> Get all grades received by a specific program user </summary>
  public async Task<IEnumerable<ActivityGrade>> GetGradesByStudentAsync(Guid programUserId) {
    return await context.Set<ActivityGrade>().Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.Content)
                        .Include(ag => ag.GraderProgramUser)
                        .ThenInclude(gpu => gpu!.User)
                        .Where(ag => ag.ContentInteraction.ProgramUserId == programUserId && ag.ContentInteraction.Content.Type != ProgramContentType.Survey)
                        .OrderByDescending(ag => ag.GradedAt)
                        .ToListAsync();
  }

  /// <summary> Update an existing grade </summary>
  public async Task<ActivityGrade?> UpdateGradeAsync(Guid gradeId, decimal? newGrade = null, string? newFeedback = null, string? newGradingDetails = null) {
    var grade = await context.Set<ActivityGrade>()
      .Include(ag => ag.ContentInteraction)
      .ThenInclude(interaction => interaction.Content)
      .FirstOrDefaultAsync(ag => ag.Id == gradeId);

    if (grade == null) return null;
    if (grade.ContentInteraction.Content.Type == ProgramContentType.Survey)
      throw new InvalidOperationException("Surveys cannot be graded.");

    if (newGrade.HasValue) grade.Grade = newGrade.Value;
    if (newFeedback != null) grade.Feedback = newFeedback;
    if (newGradingDetails != null) grade.GradingDetails = newGradingDetails;
    grade.GradedAt = SystemClock.UtcNow;
    grade.Touch();

    await context.SaveChangesAsync().ConfigureAwait(false);

    return grade;
  }

  /// <summary> Delete a grade </summary>
  public async Task<bool> DeleteGradeAsync(Guid gradeId) {
    var grade = await context.Set<ActivityGrade>().FirstOrDefaultAsync(ag => ag.Id == gradeId);

    if (grade == null) return false;

    context.Set<ActivityGrade>().Remove(grade);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return true;
  }

  /// <summary> Get all pending grades for a program (content interactions that need grading) </summary>
  public async Task<IEnumerable<ContentInteraction>> GetPendingGradesAsync(Guid programId) {
    return await context.Set<ContentInteraction>().Include(ci => ci.Content)
                        .Include(ci => ci.ProgramUser)
                        .ThenInclude(pu => pu.User)
                        .Where(ci => ci.Content.ProgramId == programId && ci.Content.Type != ProgramContentType.Survey && ci.SubmittedAt.HasValue && !context.Set<ActivityGrade>().Any(ag => ag.ContentInteractionId == ci.Id))
                        .OrderBy(ci => ci.SubmittedAt)
                        .ToListAsync();
  }

  /// <summary> Get grade statistics for a program </summary>
  public async Task<GradeStatistics> GetGradeStatisticsAsync(Guid programId) {
    var grades = await context.Set<ActivityGrade>().Include(ag => ag.ContentInteraction).ThenInclude(ci => ci.Content).Where(ag => ag.ContentInteraction.Content.ProgramId == programId && ag.ContentInteraction.Content.Type != ProgramContentType.Survey).Select(ag => ag.Grade).ToListAsync();

    if (grades.Count == 0) return new GradeStatistics { TotalGrades = 0, AverageGrade = 0, MinGrade = 0, MaxGrade = 0, PassingRate = 0 };

    return new GradeStatistics {
      TotalGrades = grades.Count, AverageGrade = grades.Average(), MinGrade = grades.Min(), MaxGrade = grades.Max(), PassingRate = grades.Count(g => g >= 60) / (decimal) grades.Count * 100, // Assuming 60 is passing
    };
  }

  /// <summary> Get grades for a specific content item across all students </summary>
  public async Task<IEnumerable<ActivityGrade>> GetGradesByContentAsync(Guid contentId) {
    return await context.Set<ActivityGrade>().Include(ag => ag.ContentInteraction)
                        .ThenInclude(ci => ci.ProgramUser)
                        .ThenInclude(pu => pu!.User)
                        .Include(ag => ag.GraderProgramUser)
                        .ThenInclude(gpu => gpu!.User)
                        .Where(ag => ag.ContentInteraction.ContentId == contentId && ag.ContentInteraction.Content.Type != ProgramContentType.Survey)
                        .OrderByDescending(ag => ag.GradedAt)
                        .ToListAsync();
  }
}
