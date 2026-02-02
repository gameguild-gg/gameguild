using GameGuild.Abstractions;


using Microsoft.EntityFrameworkCore;


namespace GameGuild.Learning.Courses;

/// <summary> Service implementation for content progress tracking </summary>
public class ContentProgressService : IContentProgressService {
  private readonly IApplicationDbContext _context;

  private readonly IProgramEnrollmentService _enrollmentService;

  public ContentProgressService(IApplicationDbContext context, IProgramEnrollmentService enrollmentService) {
    _context = context;
    _enrollmentService = enrollmentService;
  }

  /// <summary> Track user access to content </summary>
  public async Task<ContentProgress> TrackContentAccessAsync(Guid userId, Guid contentId, Guid programEnrollmentId) {
    var progress = await _context.Set<ContentProgress>().FirstOrDefaultAsync(cp => cp.UserId == userId && cp.ContentId == contentId);

    if (progress == null) {
      progress = new ContentProgress { UserId = userId, ContentId = contentId, ProgramEnrollmentId = programEnrollmentId };
      _context.Set<ContentProgress>().Add(progress);
    }

    progress.MarkAsAccessed();
    await _context.SaveChangesAsync();

    // Update program-level progress
    await UpdateProgramProgressAsync(userId, programEnrollmentId);

    return progress;
  }

  /// <summary> Start tracking content progress (initial access) </summary>
  public async Task<ContentProgress> StartContentAsync(Guid userId, Guid contentId, Guid programEnrollmentId) { return await TrackContentAccessAsync(userId, contentId, programEnrollmentId); }

  /// <summary> Update content progress </summary>
  public async Task<ContentProgress> UpdateContentProgressAsync(Guid userId, Guid contentId, decimal progressPercentage, int? timeSpentSeconds = null) {
    var progress = await _context.Set<ContentProgress>().FirstOrDefaultAsync(cp => cp.UserId == userId && cp.ContentId == contentId);

    if (progress == null) { throw new ArgumentException("Content progress not found. Track access first.", nameof(contentId)); }

    progress.UpdateProgress(progressPercentage);

    if (timeSpentSeconds.HasValue) { progress.AddTimeSpent(timeSpentSeconds.Value); }

    await _context.SaveChangesAsync();

    // Update program-level progress
    await UpdateProgramProgressAsync(userId, progress.ProgramEnrollmentId);

    return progress;
  }

  /// <summary> Mark content as completed </summary>
  public async Task<ContentProgress> CompleteContentAsync(Guid userId, Guid contentId, decimal? score = null, decimal? maxScore = null) {
    var progress = await _context.Set<ContentProgress>().FirstOrDefaultAsync(cp => cp.UserId == userId && cp.ContentId == contentId);

    if (progress == null) { throw new ArgumentException("Content progress not found. Track access first.", nameof(contentId)); }

    progress.MarkAsCompleted(score, maxScore);
    await _context.SaveChangesAsync();

    // Update program-level progress
    await UpdateProgramProgressAsync(userId, progress.ProgramEnrollmentId);

    return progress;
  }

  /// <summary> Get user's progress for specific content </summary>
  public async Task<ContentProgress?> GetContentProgressAsync(Guid userId, Guid contentId) { return await _context.Set<ContentProgress>().Include(cp => cp.Content).FirstOrDefaultAsync(cp => cp.UserId == userId && cp.ContentId == contentId); }

  /// <summary> Get user's progress for all content in a program </summary>
  public async Task<IEnumerable<ContentProgress>> GetProgramProgressAsync(Guid userId, Guid programId) {
    return await _context.Set<ContentProgress>().Include(cp => cp.Content)
                         .Join(_context.Set<ProgramContent>(), cp => cp.ContentId, pc => pc.Id, (cp, pc) => new { cp, pc })
                         .Where(x => x.cp.UserId == userId && x.pc.ProgramId == programId)
                         .Select(x => x.cp)
                         .OrderBy(cp => cp.Content!.Id) // Using Id instead of SortOrder for now
                         .ToListAsync();
  }

  /// <summary> Calculate overall program progress percentage </summary>
  public async Task<decimal> CalculateProgramProgressAsync(Guid userId, Guid programId) {
    // Get all content in the program
    var totalContent = await _context.Set<ProgramContent>().Where(pc => pc.ProgramId == programId && pc.IsRequired).CountAsync();

    if (totalContent == 0) return 0;

    // Get completed content
    var completedContent = await _context.Set<ContentProgress>().Join(_context.Set<ProgramContent>(), cp => cp.ContentId, pc => pc.Id, (cp, pc) => new { cp, pc })
                                         .Where(x => x.cp.UserId == userId && x.pc.ProgramId == programId && x.pc.IsRequired && x.cp.CompletionStatus == ContentCompletionStatus.Completed)
                                         .CountAsync();

    return totalContent > 0 ? (decimal)completedContent / totalContent * 100 : 0;
  }

  /// <summary> Get next content item to access in program </summary>
  public async Task<Guid?> GetNextContentAsync(Guid userId, Guid programId) {
    // Get program content ordered by sort order
    var programContents = await _context.Set<ProgramContent>().Where(pc => pc.ProgramId == programId).OrderBy(pc => pc.SortOrder).Select(pc => pc.Id).ToListAsync();

    // Find first content that's not completed
    foreach (var contentId in programContents) {
      var progress = await _context.Set<ContentProgress>().FirstOrDefaultAsync(cp => cp.UserId == userId && cp.ContentId == contentId);

      if (progress == null || progress.CompletionStatus != ContentCompletionStatus.Completed) {
        // Check if user can access this content (prerequisites)
        if (await CanAccessContentAsync(userId, contentId)) { return contentId; }
      }
    }

    return null; // All content completed or no accessible content
  }

  /// <summary> Check if user can access specific content (based on prerequisites) </summary>
  public async Task<bool> CanAccessContentAsync(Guid userId, Guid contentId) {
    // Get content with its program
    var content = await _context.Set<ProgramContent>().FirstOrDefaultAsync(pc => pc.Id == contentId);

    if (content == null) return false;

    // Get all content items in the same program, ordered by sort order
    var programContents = await _context.Set<ProgramContent>()
        .Where(pc => pc.ProgramId == content.ProgramId)
        .OrderBy(pc => pc.SortOrder)
        .ToListAsync();

    // Find the index of the current content
    var contentIndex = programContents.FindIndex(pc => pc.Id == contentId);
    
    // First item is always accessible
    if (contentIndex <= 0) return true;

    // Check if all previous required content items are completed
    var previousRequiredContents = programContents
        .Take(contentIndex)
        .Where(pc => pc.IsRequired)
        .Select(pc => pc.Id)
        .ToList();

    if (previousRequiredContents.Count == 0) return true;

    // Get user progress for previous required content
    var completedCount = await _context.Set<ContentProgress>()
        .Where(cp => cp.UserId == userId 
            && previousRequiredContents.Contains(cp.ContentId)
            && cp.CompletionStatus == ContentCompletionStatus.Completed)
        .CountAsync();

    // User can access if all previous required content is completed
    return completedCount >= previousRequiredContents.Count;
  }

  /// <summary> Get content completion statistics for a program </summary>
  public async Task<ContentCompletionStats> GetCompletionStatsAsync(Guid programId) {
    var allProgress = await _context.Set<ContentProgress>().Include(cp => cp.Content)
                                    .Join(_context.Set<ProgramContent>(), cp => cp.ContentId, pc => pc.Id, (cp, pc) => new { cp, pc })
                                    .Where(x => x.pc.ProgramId == programId)
                                    .Select(x => x.cp)
                                    .ToListAsync();

    var totalContent = await _context.Set<ProgramContent>().Where(pc => pc.ProgramId == programId).CountAsync();

    var completed = allProgress.Count(cp => cp.CompletionStatus == ContentCompletionStatus.Completed);
    var inProgress = allProgress.Count(cp => cp.CompletionStatus == ContentCompletionStatus.InProgress);
    var notStarted = totalContent - allProgress.Count;

    var completionByType = allProgress.GroupBy(cp => "Content") // Using generic type for now
                                      .ToDictionary(g => g.Key, g => g.Count(cp => cp.CompletionStatus == ContentCompletionStatus.Completed));

    return new ContentCompletionStats {
      TotalContentItems = totalContent,
      CompletedContentItems = completed,
      InProgressContentItems = inProgress,
      NotStartedContentItems = notStarted,
      AverageCompletionRate = totalContent > 0 ? (decimal)completed / totalContent * 100 : 0,
      AverageScore = allProgress.Where(cp => cp.Score.HasValue).Any() ? allProgress.Where(cp => cp.Score.HasValue).Average(cp => cp.Score!.Value) : 0,
      TotalTimeSpentHours = allProgress.Sum(cp => cp.TimeSpentSeconds) / 3600,
      CompletionByContentType = completionByType,
    };
  }

  /// <summary> Reset user progress for a program (admin function) </summary>
  public async Task<bool> ResetProgramProgressAsync(Guid userId, Guid programId) {
    var progressRecords = await _context.Set<ContentProgress>().Join(_context.Set<ProgramContent>(), cp => cp.ContentId, pc => pc.Id, (cp, pc) => new { cp, pc })
                                        .Where(x => x.cp.UserId == userId && x.pc.ProgramId == programId)
                                        .Select(x => x.cp)
                                        .ToListAsync();

    _context.Set<ContentProgress>().RemoveRange(progressRecords);
    await _context.SaveChangesAsync();

    // Reset program enrollment progress
    var enrollment = await _context.Set<ProgramEnrollment>().FirstOrDefaultAsync(pe => pe.UserId == userId && pe.ProgramId == programId);

    if (enrollment != null) {
      enrollment.ProgressPercentage = 0;
      enrollment.CompletionStatus = CompletionStatus.NotStarted;
      enrollment.CompletedAt = null;
      enrollment.Touch();
      await _context.SaveChangesAsync();
    }

    return true;
  }

  /// <summary> Update program-level progress based on content completion </summary>
  private async Task UpdateProgramProgressAsync(Guid userId, Guid programEnrollmentId) {
    var enrollment = await _context.Set<ProgramEnrollment>().FirstOrDefaultAsync(pe => pe.Id == programEnrollmentId);

    if (enrollment == null) return;

    var programProgress = await CalculateProgramProgressAsync(userId, enrollment.ProgramId);
    await _enrollmentService.UpdateProgressAsync(programEnrollmentId, programProgress);
  }
}
