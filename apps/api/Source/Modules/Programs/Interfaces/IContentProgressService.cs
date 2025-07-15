namespace GameGuild.Modules.Programs;

/// <summary>
/// Interface for content progress tracking services
/// </summary>
public interface IContentProgressService {
  /// <summary>
  /// Track user access to content
  /// </summary>
  Task<ContentProgress> TrackContentAccessAsync(Guid userId, Guid contentId, Guid programEnrollmentId);

  /// <summary>
  /// Start tracking content progress (initial access)
  /// </summary>
  Task<ContentProgress> StartContentAsync(Guid userId, Guid contentId, Guid programEnrollmentId);

  /// <summary>
  /// Update content progress
  /// </summary>
  Task<ContentProgress> UpdateContentProgressAsync(Guid userId, Guid contentId, decimal progressPercentage, int? timeSpentSeconds = null);

  /// <summary>
  /// Mark content as completed
  /// </summary>
  Task<ContentProgress> CompleteContentAsync(Guid userId, Guid contentId, decimal? score = null, decimal? maxScore = null);

  /// <summary>
  /// Get user's progress for specific content
  /// </summary>
  Task<ContentProgress?> GetContentProgressAsync(Guid userId, Guid contentId);

  /// <summary>
  /// Get user's progress for all content in a program
  /// </summary>
  Task<IEnumerable<ContentProgress>> GetProgramProgressAsync(Guid userId, Guid programId);

  /// <summary>
  /// Calculate overall program progress percentage
  /// </summary>
  Task<decimal> CalculateProgramProgressAsync(Guid userId, Guid programId);

  /// <summary>
  /// Get next content item to access in program
  /// </summary>
  Task<Guid?> GetNextContentAsync(Guid userId, Guid programId);

  /// <summary>
  /// Check if user can access specific content (based on prerequisites)
  /// </summary>
  Task<bool> CanAccessContentAsync(Guid userId, Guid contentId);

  /// <summary>
  /// Get content completion statistics for a program
  /// </summary>
  Task<ContentCompletionStats> GetCompletionStatsAsync(Guid programId);

  /// <summary>
  /// Reset user progress for a program (admin function)
  /// </summary>
  Task<bool> ResetProgramProgressAsync(Guid userId, Guid programId);
}

/// <summary>
/// Content completion statistics
/// </summary>
public class ContentCompletionStats {
  public int TotalContentItems { get; set; }

  public int CompletedContentItems { get; set; }

  public int InProgressContentItems { get; set; }

  public int NotStartedContentItems { get; set; }

  public decimal AverageCompletionRate { get; set; }

  public decimal AverageScore { get; set; }

  public int TotalTimeSpentHours { get; set; }

  public Dictionary<string, int> CompletionByContentType { get; set; } = new();
}
