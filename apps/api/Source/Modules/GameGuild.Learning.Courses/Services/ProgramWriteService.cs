using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Write-side service for Programs: create, update, delete, clone,
/// content management, user management, progress mutations, monetization, and product integration.
/// </summary>
public class ProgramWriteService(IApplicationDbContext context) : IProgramWriteService
{
  // ── Program CRUD ────────────────────────────────────────────────────

  public async Task<Program> CreateProgramAsync(Program program)
  {
    program.Status = ContentStatus.Draft;
    program.Visibility = ContentVisibility.Private;

    context.Set<Program>().Add(program);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program> UpdateProgramAsync(Program program)
  {
    program.Touch();
    context.Set<Program>().Update(program);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task DeleteProgramAsync(Guid id)
  {
    var program = await context.Set<Program>().FindAsync(id).ConfigureAwait(false);

    if (program != null)
    {
      program.SoftDelete();
      await context.SaveChangesAsync().ConfigureAwait(false);
    }
  }

  public async Task<Program> CloneProgramAsync(Guid id, string newTitle)
  {
    var originalProgram = await context.Set<Program>()
      .Include(p => p.ProgramContents.Where(pc => pc.DeletedAt == null))
      .Include(p => p.ProgramUsers.Where(pu => pu.DeletedAt == null))
      .Where(p => p.DeletedAt == null)
      .FirstOrDefaultAsync(p => p.Id == id);

    if (originalProgram == null) throw new ArgumentException("Program not found", nameof(id));

    var clonedProgram = new Program
    {
      CreatorId = originalProgram.CreatorId,
      Title = newTitle,
      Description = originalProgram.Description,
      Slug = GenerateSlug(newTitle),
      Thumbnail = originalProgram.Thumbnail,
      Status = ContentStatus.Draft,
      Visibility = ContentVisibility.Private,
    };

    context.Set<Program>().Add(clonedProgram);
    await context.SaveChangesAsync().ConfigureAwait(false);

    // Clone content
    foreach (var content in originalProgram.ProgramContents.OrderBy(pc => pc.SortOrder))
    {
      var clonedContent = new ProgramContent
      {
        ProgramId = clonedProgram.Id,
        Title = content.Title,
        Description = content.Description,
        Type = content.Type,
        Body = content.Body,
        SortOrder = content.SortOrder,
        IsRequired = content.IsRequired,
        GradingMethod = content.GradingMethod,
        MaxPoints = content.MaxPoints,
        EstimatedMinutes = content.EstimatedMinutes,
        Visibility = content.Visibility,
      };

      context.Set<ProgramContent>().Add(clonedContent);
    }

    await context.SaveChangesAsync().ConfigureAwait(false);

    return clonedProgram;
  }

  // ── CRUD with DTOs ──────────────────────────────────────────────────

  public async Task<Program> CreateProgramAsync(CreateProgramDto createDto)
  {
    var program = new Program
    {
      Id = Guid.NewGuid(),
      CreatorId = createDto.CreatorId,
      Title = createDto.Title,
      Description = createDto.Description,
      Slug = createDto.Slug,
      Thumbnail = createDto.Thumbnail,
      Status = ContentStatus.Draft,
      Visibility = ContentVisibility.Private,
    };

    context.Set<Program>().Add(program);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program?> UpdateProgramAsync(Guid id, UpdateProgramDto updateDto)
  {
    var program = await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == id).ConfigureAwait(false);

    if (program == null) return null;

    if (updateDto.Title != null) program.Title = updateDto.Title;
    if (updateDto.Description != null) program.Description = updateDto.Description;
    if (updateDto.Metadata != null) program.Metadata = updateDto.Metadata;
    if (updateDto.Slug != null) program.Slug = updateDto.Slug;
    if (updateDto.Thumbnail != null) program.Thumbnail = updateDto.Thumbnail;
    if (updateDto.VideoShowcaseUrl != null) program.VideoShowcaseUrl = updateDto.VideoShowcaseUrl;
    if (updateDto.EstimatedHours.HasValue) program.EstimatedHours = updateDto.EstimatedHours.Value;
    if (updateDto.Visibility.HasValue) program.Visibility = updateDto.Visibility.Value;
    if (updateDto.Category.HasValue) program.Category = updateDto.Category.Value;
    if (updateDto.Difficulty.HasValue) program.Difficulty = updateDto.Difficulty.Value;
    if (updateDto.SkillsRequired != null) program.SkillsRequired = updateDto.SkillsRequired;
    if (updateDto.SkillsProvided != null) program.SkillsProvided = updateDto.SkillsProvided;
    if (updateDto.EnrollmentStatus.HasValue) program.EnrollmentStatus = updateDto.EnrollmentStatus.Value;
    if (updateDto.MaxEnrollments.HasValue) program.MaxEnrollments = updateDto.MaxEnrollments.Value;
    if (updateDto.EnrollmentDeadline.HasValue) program.EnrollmentDeadline = updateDto.EnrollmentDeadline.Value;

    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  // ── Content Management ──────────────────────────────────────────────

  public async Task<ProgramContent> AddContentAsync(Guid programId, ProgramContent content)
  {
    var program = await context.Set<Program>().Where(p => p.DeletedAt == null).AnyAsync(p => p.Id == programId).ConfigureAwait(false);

    if (!program) throw new ArgumentException("Program not found", nameof(programId));

    content.ProgramId = programId;

    if (content.SortOrder == 0)
    {
      var maxOrder = await context.Set<ProgramContent>().Where(pc => pc.DeletedAt == null && pc.ProgramId == programId).MaxAsync(pc => (int?)pc.SortOrder) ?? 0;
      content.SortOrder = maxOrder + 1;
    }

    context.Set<ProgramContent>().Add(content);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return content;
  }

  public async Task<ProgramContent> UpdateContentAsync(ProgramContent content)
  {
    content.Touch();
    context.Set<ProgramContent>().Update(content);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return content;
  }

  public async Task DeleteContentAsync(Guid contentId)
  {
    var content = await context.Set<ProgramContent>().FindAsync(contentId).ConfigureAwait(false);

    if (content != null)
    {
      content.SoftDelete();
      await context.SaveChangesAsync().ConfigureAwait(false);
    }
  }

  public async Task<Program> ReorderContentAsync(Guid programId, List<Guid> contentIds)
  {
    var program = await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == programId).ConfigureAwait(false);

    if (program == null) throw new ArgumentException("Program not found", nameof(programId));

    var contents = await context.Set<ProgramContent>().Where(pc => pc.DeletedAt == null && pc.ProgramId == programId && contentIds.Contains(pc.Id)).ToListAsync();

    for (var i = 0; i < contentIds.Count; i++)
    {
      var content = contents.FirstOrDefault(c => c.Id == contentIds[i]);

      if (content != null)
      {
        content.SortOrder = i + 1;
        content.Touch();
      }
    }

    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<ProgramContent?> AddContentAsync(Guid programId, CreateContentDto contentDto)
  {
    var program = await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == programId).ConfigureAwait(false);

    if (program == null) return null;

    var content = new ProgramContent
    {
      Id = Guid.NewGuid(),
      ProgramId = programId,
      Title = contentDto.Title,
      Description = contentDto.Description,
      Type = contentDto.Type,
      Body = contentDto.Body,
      SortOrder = contentDto.SortOrder ?? 0,
      IsRequired = contentDto.IsRequired,
      EstimatedMinutes = contentDto.EstimatedMinutes,
    };

    context.Set<ProgramContent>().Add(content);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return content;
  }

  public async Task<ProgramContent?> UpdateContentAsync(Guid programId, Guid contentId, UpdateContentDto contentDto)
  {
    var content = await context.Set<ProgramContent>().FirstOrDefaultAsync(c => c.Id == contentId && c.ProgramId == programId && c.DeletedAt == null);

    if (content == null) return null;

    if (contentDto.Title != null) content.Title = contentDto.Title;
    if (contentDto.Description != null) content.Description = contentDto.Description;
    if (contentDto.Body != null) content.Body = contentDto.Body;
    if (contentDto.SortOrder != null) content.SortOrder = contentDto.SortOrder.Value;
    if (contentDto.IsRequired != null) content.IsRequired = contentDto.IsRequired.Value;
    if (contentDto.EstimatedMinutes != null) content.EstimatedMinutes = contentDto.EstimatedMinutes;

    content.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return content;
  }

  public async Task<bool> RemoveContentAsync(Guid programId, Guid contentId)
  {
    var content = await context.Set<ProgramContent>().FirstOrDefaultAsync(c => c.Id == contentId && c.ProgramId == programId && c.DeletedAt == null);

    if (content == null) return false;

    content.SoftDelete();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return true;
  }

  // ── User Management ─────────────────────────────────────────────────

  public async Task<ProgramUser> AddUserAsync(Guid programId, Guid userId)
  {
    var existingUser = await context.Set<ProgramUser>().Where(pu => pu.DeletedAt == null && pu.ProgramId == programId && pu.UserId == userId).FirstOrDefaultAsync();

    if (existingUser != null)
    {
      if (!existingUser.IsActive)
      {
        existingUser.IsActive = true;
        existingUser.JoinedAt = SystemClock.UtcNow;
        existingUser.Touch();
        await context.SaveChangesAsync().ConfigureAwait(false);
      }

      return existingUser;
    }

    var programUser = new ProgramUser { ProgramId = programId, UserId = userId, IsActive = true, JoinedAt = SystemClock.UtcNow };

    context.Set<ProgramUser>().Add(programUser);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return programUser;
  }

  public async Task<ProgramUser> RemoveUserAsync(Guid programId, Guid userId)
  {
    var programUser = await context.Set<ProgramUser>().Where(pu => pu.DeletedAt == null && pu.ProgramId == programId && pu.UserId == userId).FirstOrDefaultAsync();

    if (programUser != null)
    {
      programUser.IsActive = false;
      programUser.Touch();
      await context.SaveChangesAsync().ConfigureAwait(false);
    }

    return programUser!;
  }

  public async Task<UserProgressDto?> AddUserToProgramAsync(Guid programId, Guid userId)
  {
    var program = await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == programId).ConfigureAwait(false);

    if (program == null) return null;

    var existingUser = await context.Set<ProgramUser>().FirstOrDefaultAsync(pu => pu.ProgramId == programId && pu.UserId == userId && pu.DeletedAt == null);

    if (existingUser != null)
      return await GetUserProgressDtoInternalAsync(programId, userId).ConfigureAwait(false);

    var programUser = new ProgramUser
    {
      Id = Guid.NewGuid(),
      ProgramId = programId,
      UserId = userId,
      JoinedAt = SystemClock.UtcNow,
      LastAccessedAt = SystemClock.UtcNow,
      CompletionPercentage = 0,
    };

    context.Set<ProgramUser>().Add(programUser);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return await GetUserProgressDtoInternalAsync(programId, userId).ConfigureAwait(false);
  }

  public async Task<bool> RemoveUserFromProgramAsync(Guid programId, Guid userId)
  {
    var programUser = await context.Set<ProgramUser>().FirstOrDefaultAsync(pu => pu.ProgramId == programId && pu.UserId == userId && pu.DeletedAt == null);

    if (programUser == null) return false;

    programUser.SoftDelete();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return true;
  }

  // ── Progress Mutations ──────────────────────────────────────────────

  public async Task<Program> UpdateUserProgressAsync(Guid programId, Guid userId, Guid contentId, ProgressStatus status)
  {
    var program = await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == programId).ConfigureAwait(false);

    if (program == null) throw new ArgumentException("Program not found", nameof(programId));

    var programUser = await context.Set<ProgramUser>().Where(pu => pu.DeletedAt == null && pu.ProgramId == programId && pu.UserId == userId).FirstOrDefaultAsync();

    if (programUser == null) throw new ArgumentException("User not enrolled in program");

    var interaction = await context.Set<ContentInteraction>().Where(ci => ci.DeletedAt == null && ci.ProgramUserId == programUser.Id && ci.ContentId == contentId).FirstOrDefaultAsync();

    if (interaction == null)
    {
      interaction = new ContentInteraction { ProgramUserId = programUser.Id, ContentId = contentId, Status = status, FirstAccessedAt = SystemClock.UtcNow, LastAccessedAt = SystemClock.UtcNow, };

      context.Set<ContentInteraction>().Add(interaction);
    }
    else
    {
      interaction.Status = status;
      interaction.LastAccessedAt = SystemClock.UtcNow;

      if (status == ProgressStatus.Completed && interaction.CompletedAt == null)
      {
        interaction.CompletedAt = SystemClock.UtcNow;
        interaction.CompletionPercentage = 100;
      }

      interaction.Touch();
    }

    await RecalculateUserProgressAsync(programUser.Id).ConfigureAwait(false);

    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<UserProgressDto?> UpdateUserProgressAsync(Guid programId, Guid userId, UpdateProgressDto progressDto)
  {
    var programUser = await context.Set<ProgramUser>().FirstOrDefaultAsync(pu => pu.ProgramId == programId && pu.UserId == userId && pu.DeletedAt == null);

    if (programUser == null) return null;

    if (progressDto.LastAccessedAt != null) programUser.LastAccessedAt = progressDto.LastAccessedAt.Value;
    programUser.Touch();

    await context.SaveChangesAsync().ConfigureAwait(false);

    return await GetUserProgressDtoInternalAsync(programId, userId).ConfigureAwait(false);
  }

  public async Task<ContentInteraction?> SubmitUserContentAsync(Guid programId, Guid userId, Guid contentId, string submissionData)
  {
    var programUser = await context.Set<ProgramUser>()
      .FirstOrDefaultAsync(pu => pu.ProgramId == programId && pu.UserId == userId && pu.DeletedAt == null && pu.IsActive)
      .ConfigureAwait(false);

    if (programUser == null) return null;

    var content = await context.Set<ProgramContent>()
      .FirstOrDefaultAsync(pc => pc.Id == contentId && pc.ProgramId == programId && pc.DeletedAt == null)
      .ConfigureAwait(false);

    if (content == null) return null;

    var now = SystemClock.UtcNow;
    var interaction = await context.Set<ContentInteraction>()
      .FirstOrDefaultAsync(ci => ci.ProgramUserId == programUser.Id && ci.ContentId == contentId && ci.DeletedAt == null)
      .ConfigureAwait(false);

    if (interaction?.SubmittedAt != null)
    {
      return interaction;
    }

    if (interaction == null)
    {
      interaction = new ContentInteraction
      {
        ProgramUserId = programUser.Id,
        UserId = userId,
        ContentId = contentId,
        Status = ProgressStatus.InProgress,
        FirstAccessedAt = now,
        LastAccessedAt = now,
        StartedAt = now,
        CompletionPercentage = 0,
      };

      context.Set<ContentInteraction>().Add(interaction);
    }

    interaction.SubmissionData = submissionData;
    interaction.SubmittedAt = now;
    interaction.LastAccessedAt = now;
    interaction.StartedAt ??= now;
    interaction.Status = ProgressStatus.Completed;
    interaction.CompletedAt = now;
    interaction.CompletionPercentage = 100;
    interaction.IsCompleted = true;
    interaction.AttemptCount = Math.Max(1, interaction.AttemptCount + 1);
    interaction.Touch();

    programUser.LastAccessedAt = now;
    programUser.Touch();

    await RecalculateUserProgressAsync(programUser.Id).ConfigureAwait(false);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return interaction;
  }

  public async Task<bool> MarkContentCompletedAsync(Guid programId, Guid userId, Guid contentId)
  {
    var programUser = await context.Set<ProgramUser>().FirstOrDefaultAsync(pu => pu.ProgramId == programId && pu.UserId == userId && pu.DeletedAt == null);

    if (programUser == null) return false;

    await RecalculateUserProgressAsync(programUser.Id).ConfigureAwait(false);

    return true;
  }

  public async Task<bool> ResetUserProgressAsync(Guid programId, Guid userId)
  {
    var programUser = await context.Set<ProgramUser>().FirstOrDefaultAsync(pu => pu.ProgramId == programId && pu.UserId == userId && pu.DeletedAt == null);

    if (programUser == null) return false;

    programUser.CompletionPercentage = 0;
    programUser.CompletedAt = null;
    programUser.LastAccessedAt = SystemClock.UtcNow;
    programUser.Touch();

    await context.SaveChangesAsync().ConfigureAwait(false);

    return true;
  }

  // ── Monetization ────────────────────────────────────────────────────

  public async Task<Program?> EnableMonetizationAsync(Guid id, MonetizationDto monetizationDto)
  {
    var program = await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == id).ConfigureAwait(false);

    if (program == null) return null;

    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<Program?> DisableMonetizationAsync(Guid id)
  {
    var program = await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == id).ConfigureAwait(false);

    if (program == null) return null;

    program.Touch();
    await context.SaveChangesAsync().ConfigureAwait(false);

    return program;
  }

  public async Task<PricingDto?> UpdateProgramPricingAsync(Guid id, UpdatePricingDto pricingDto)
  {
    var program = await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == id).ConfigureAwait(false);

    if (program == null) return null;

    return new PricingDto(0, "USD", false, null, false);
  }

  // ── Product Integration ─────────────────────────────────────────────

  public async Task<Guid?> CreateProductFromProgramAsync(Guid programId, CreateProductFromProgramDto productDto)
  {
    var program = await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == programId).ConfigureAwait(false);

    if (program == null) return null;

    return Guid.NewGuid();
  }

  public async Task<bool> LinkProgramToProductAsync(Guid programId, Guid productId)
  {
    var program = await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == programId).ConfigureAwait(false);

    if (program == null) return false;

    return true;
  }

  public async Task<bool> UnlinkProgramFromProductAsync(Guid programId, Guid productId)
  {
    var program = await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == programId).ConfigureAwait(false);

    if (program == null) return false;

    return true;
  }

  // ── Private Helpers ─────────────────────────────────────────────────

  private static string GenerateSlug(string title) { return title.ToLowerInvariant().Replace(" ", "-").Replace("'", "").Replace("\"", ""); }

  private async Task RecalculateUserProgressAsync(Guid programUserId)
  {
    var programUser = await context.Set<ProgramUser>().Where(pu => pu.Id == programUserId).FirstOrDefaultAsync();

    if (programUser == null) return;

    var totalContent = await context.Set<ProgramContent>().Where(pc => pc.DeletedAt == null && pc.ProgramId == programUser.ProgramId && pc.IsRequired).CountAsync();

    if (totalContent == 0)
    {
      programUser.CompletionPercentage = 0;

      return;
    }

    var completedContent = await context.Set<ContentInteraction>().Where(ci => ci.DeletedAt == null && ci.ProgramUserId == programUserId && ci.Status == ProgressStatus.Completed).CountAsync();

    programUser.CompletionPercentage = (decimal)completedContent / totalContent * 100;

    if (programUser is { CompletionPercentage: >= 100, CompletedAt: null }) programUser.CompletedAt = SystemClock.UtcNow;

    programUser.Touch();
  }

  /// <summary>Internal helper to build a <see cref="UserProgressDto"/> without depending on the read service.</summary>
  private async Task<UserProgressDto?> GetUserProgressDtoInternalAsync(Guid programId, Guid userId)
  {
    var programUser = await context.Set<ProgramUser>().FirstOrDefaultAsync(pu => pu.ProgramId == programId && pu.UserId == userId && pu.DeletedAt == null);

    if (programUser == null) return null;

    var contentProgress = new List<ContentProgressDto>();

    return new UserProgressDto(
      programUser.Id,
      programUser.ProgramId,
      programUser.UserId,
      programUser.CompletionPercentage,
      programUser.LastAccessedAt,
      programUser.StartedAt,
      programUser.CompletedAt,
      contentProgress
    );
  }
}
