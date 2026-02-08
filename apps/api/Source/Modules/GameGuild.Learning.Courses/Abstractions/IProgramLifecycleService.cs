namespace GameGuild.Learning.Courses;

/// <summary>
/// Service interface for Program lifecycle management:
/// draft, review, approve, reject, archive, restore, publish, unpublish, schedule, visibility.
/// </summary>
public interface IProgramLifecycleService {
  // Lifecycle state transitions
  Task<Program> CreateDraftAsync(Program program);
  Task<Program> SubmitForReviewAsync(Guid id);
  Task<Program> ApproveAsync(Guid id);
  Task<Program> RejectAsync(Guid id, string reason);
  Task<Program> ArchiveAsync(Guid id);
  Task<Program> RestoreAsync(Guid id);
  Task<Program> PublishAsync(Guid id);
  Task<Program> SetVisibilityAsync(Guid id, ContentVisibility visibility);

  // Publishing operations
  Task<Program> PublishProgramAsync(Guid id);
  Task<Program> UnpublishProgramAsync(Guid id);
  Task<Program> SchedulePublishAsync(Guid id, DateTime publishAt);

  // Lifecycle with null return (controller-friendly)
  Task<Program?> SubmitProgramAsync(Guid id);
  Task<Program?> ApproveProgramAsync(Guid id);
  Task<Program?> RejectProgramAsync(Guid id, string reason);
  Task<Program?> WithdrawProgramAsync(Guid id);
  Task<Program?> ArchiveProgramAsync(Guid id);
  Task<Program?> RestoreProgramAsync(Guid id);
  Task<Program?> ScheduleProgramAsync(Guid id, DateTime publishAt);
}
