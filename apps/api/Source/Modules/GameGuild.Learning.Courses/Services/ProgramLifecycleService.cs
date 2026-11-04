using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Service implementation for Program lifecycle management:
/// draft, review, approve, reject, archive, restore, publish, unpublish, schedule, visibility.
/// </summary>
public class ProgramLifecycleService(IApplicationDbContext context) : IProgramLifecycleService {

  // ── Lifecycle State Transitions ─────────────────────────────────────

  public async Task<Program> CreateDraftAsync(Program program) {
    program.Status = ContentStatus.Draft;
    program.Visibility = ContentVisibility.Private;

    context.Set<Program>().Add(program);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program> SubmitForReviewAsync(Guid id) {
    var program = await GetRequiredProgramAsync(id).ConfigureAwait(false);

    program.Status = ContentStatus.Review;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program> ApproveAsync(Guid id) {
    var program = await GetRequiredProgramAsync(id).ConfigureAwait(false);

    program.Status = ContentStatus.Published;
    program.Visibility = ContentVisibility.Public;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program> RejectAsync(Guid id, string reason) {
    var program = await GetRequiredProgramAsync(id).ConfigureAwait(false);

    program.Status = ContentStatus.Draft;
    program.SetMetadata("rejectionReason", reason);
    program.SetMetadata("rejectionDate", SystemClock.UtcNow);
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program> ArchiveAsync(Guid id) {
    var program = await GetRequiredProgramAsync(id).ConfigureAwait(false);

    program.Status = ContentStatus.Archived;
    program.Visibility = ContentVisibility.Private;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program> RestoreAsync(Guid id) {
    var program = await GetRequiredProgramAsync(id).ConfigureAwait(false);

    program.Status = ContentStatus.Draft;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program> PublishAsync(Guid id) {
    var program = await GetRequiredProgramAsync(id).ConfigureAwait(false);

    program.Status = ContentStatus.Published;
    program.Visibility = ContentVisibility.Public;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program> SetVisibilityAsync(Guid id, ContentVisibility visibility) {
    var program = await GetRequiredProgramAsync(id).ConfigureAwait(false);

    program.Visibility = visibility;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  // ── Publishing Operations ───────────────────────────────────────────

  public async Task<Program> PublishProgramAsync(Guid id) {
    var program = await GetRequiredProgramAsync(id).ConfigureAwait(false);

    program.Status = ContentStatus.Published;
    program.Visibility = ContentVisibility.Public;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program> UnpublishProgramAsync(Guid id) {
    var program = await GetRequiredProgramAsync(id).ConfigureAwait(false);

    program.Status = ContentStatus.Draft;
    program.Visibility = ContentVisibility.Private;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program> SchedulePublishAsync(Guid id, DateTime publishAt) {
    var program = await GetRequiredProgramAsync(id).ConfigureAwait(false);

    await ScheduleAsync(program, publishAt).ConfigureAwait(false);

    return program;
  }

  // ── Lifecycle with Null Return (Controller-Friendly) ────────────────

  public async Task<Program?> SubmitProgramAsync(Guid id) {
    var program = await GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null)
        {
            return null;
        }

        program.Status = ContentStatus.Review;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program?> ApproveProgramAsync(Guid id) {
    var program = await GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null)
        {
            return null;
        }

        program.Status = ContentStatus.Published;
    program.Visibility = ContentVisibility.Public;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program?> RejectProgramAsync(Guid id, string reason) {
    var program = await GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null)
        {
            return null;
        }

        program.Status = ContentStatus.Draft;
    program.Visibility = ContentVisibility.Private;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program?> WithdrawProgramAsync(Guid id) {
    var program = await GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null)
        {
            return null;
        }

        program.Status = ContentStatus.Draft;
    program.Visibility = ContentVisibility.Private;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program?> ArchiveProgramAsync(Guid id) {
    var program = await GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null)
        {
            return null;
        }

        program.Status = ContentStatus.Archived;
    program.Visibility = ContentVisibility.Private;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program?> RestoreProgramAsync(Guid id) {
    var program = await GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null)
        {
            return null;
        }

        program.Status = ContentStatus.Draft;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program?> ScheduleProgramAsync(Guid id, DateTime publishAt) {
    var program = await GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null)
        {
            return null;
        }

    await ScheduleAsync(program, publishAt).ConfigureAwait(false);

    return program;
  }

  // ── Private Helpers ─────────────────────────────────────────────────

  /// <summary>
  /// Shared scheduling semantics (issue #1226 N10): a publish time already due is
  /// applied immediately; a future time only records the schedule — the status and
  /// visibility stay untouched and the scheduled-publish worker realizes the
  /// transition when the instant arrives.
  /// </summary>
  private async Task ScheduleAsync(Program program, DateTime publishAt) {
    var normalized = publishAt.ToUniversalTime();

    if (normalized <= SystemClock.UtcNow) {
      program.Status = ContentStatus.Published;
      program.Visibility = ContentVisibility.Public;
      program.ScheduledPublishAt = null;
      program.Touch();
      await context.SaveChangesAsync().ConfigureAwait(false);
      return;
    }

    program.ScheduledPublishAt = normalized;
    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  private async Task<Program?> GetProgramByIdAsync(Guid id) {
    return await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == id);
  }

  private async Task<Program> GetRequiredProgramAsync(Guid id) {
    var program = await GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null)
        {
            throw new ArgumentException($"Program with ID {id} not found", nameof(id));
        }

        return program;
  }
}
