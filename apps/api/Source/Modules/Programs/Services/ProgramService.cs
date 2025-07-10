using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Programs.DTOs;
using GameGuild.Modules.Programs.Models;
using Microsoft.EntityFrameworkCore;
using ProgramEntity = GameGuild.Modules.Programs.Models.Program;


namespace GameGuild.Modules.Programs.Services;

/// <summary>
/// Service implementation for Program business logic
/// Provides operations for managing programs, content, user participation, and analytics
/// </summary>
public class ProgramService(ApplicationDbContext context) : IProgramService {
  // Basic CRUD Operations
  public async Task<ProgramEntity?> GetProgramByIdAsync(Guid id) { return await context.Programs.Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == id); }

  public async Task<ProgramEntity?> GetProgramBySlugAsync(string slug) { 
    return await context.Programs.Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Slug == slug); 
  }

  public async Task<ProgramEntity?> GetProgramWithContentAsync(Guid id) {
    return await context.Programs.Include(p => p.ProgramContents.Where(pc => !pc.IsDeleted))
                        .Include(p => p.ProgramUsers.Where(pu => !pu.IsDeleted))
                        .Where(p => !p.IsDeleted)
                        .FirstOrDefaultAsync(p => p.Id == id);
  }

  public async Task<IEnumerable<ProgramEntity>> GetProgramsAsync(int skip = 0, int take = 50) {
    return await context.Programs.Where(p => p.DeletedAt == null)
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync();
  }

  public async Task<ProgramEntity> CreateProgramAsync(ProgramEntity program) {
    program.Status = ContentStatus.Draft;
    program.Visibility = AccessLevel.Private;

    context.Programs.Add(program);
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity> UpdateProgramAsync(ProgramEntity program) {
    program.Touch();
    context.Programs.Update(program);
    await context.SaveChangesAsync();

    return program;
  }

  public async Task DeleteProgramAsync(Guid id) {
    var program = await context.Programs.FindAsync(id);

    if (program != null) {
      program.SoftDelete();
      await context.SaveChangesAsync();
    }
  }

  public async Task<ProgramEntity> CloneProgramAsync(Guid id, string newTitle) {
    var originalProgram = await GetProgramWithContentAsync(id);

    if (originalProgram == null) throw new ArgumentException("Program not found", nameof(id));

    var clonedProgram = new ProgramEntity {
      Title = newTitle,
      Description = originalProgram.Description,
      Slug = GenerateSlug(newTitle),
      Thumbnail = originalProgram.Thumbnail,
      Status = ContentStatus.Draft,
      Visibility = AccessLevel.Private,
    };

    context.Programs.Add(clonedProgram);
    await context.SaveChangesAsync();

    // Clone content
    foreach (var content in originalProgram.ProgramContents.OrderBy(pc => pc.SortOrder)) {
      var clonedContent = new ProgramContent {
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

      context.ProgramContents.Add(clonedContent);
    }

    await context.SaveChangesAsync();

    return clonedProgram;
  }

  public async Task<bool> ProgramExistsAsync(Guid id) { return await context.Programs.Where(p => !p.IsDeleted).AnyAsync(p => p.Id == id); }

  // Content Management Operations
  public async Task<ProgramContent> AddContentAsync(Guid programId, ProgramContent content) {
    var program = await ProgramExistsAsync(programId);

    if (!program) throw new ArgumentException("Program not found", nameof(programId));

    content.ProgramId = programId;

    // Auto-assign sort order if not provided
    if (content.SortOrder == 0) {
      var maxOrder = await context.ProgramContents.Where(pc => !pc.IsDeleted && pc.ProgramId == programId)
                                  .MaxAsync(pc => (int?)pc.SortOrder) ??
                     0;
      content.SortOrder = maxOrder + 1;
    }

    context.ProgramContents.Add(content);
    await context.SaveChangesAsync();

    return content;
  }

  public async Task<ProgramContent> UpdateContentAsync(ProgramContent content) {
    content.Touch();
    context.ProgramContents.Update(content);
    await context.SaveChangesAsync();

    return content;
  }

  public async Task DeleteContentAsync(Guid contentId) {
    var content = await context.ProgramContents.FindAsync(contentId);

    if (content != null) {
      content.SoftDelete();
      await context.SaveChangesAsync();
    }
  }

  public async Task<ProgramEntity> ReorderContentAsync(Guid programId, List<Guid> contentIds) {
    var program = await GetProgramByIdAsync(programId);

    if (program == null) throw new ArgumentException("Program not found", nameof(programId));

    var contents = await context.ProgramContents
                                .Where(pc => !pc.IsDeleted && pc.ProgramId == programId && contentIds.Contains(pc.Id))
                                .ToListAsync();

    for (var i = 0; i < contentIds.Count; i++) {
      var content = contents.FirstOrDefault(c => c.Id == contentIds[i]);

      if (content != null) {
        content.SortOrder = i + 1;
        content.Touch();
      }
    }

    await context.SaveChangesAsync();

    return program;
  }

  public async Task<IEnumerable<ProgramContent>> GetProgramContentAsync(Guid programId) {
    return await context.ProgramContents.Where(pc => !pc.IsDeleted && pc.ProgramId == programId)
                        .OrderBy(pc => pc.SortOrder)
                        .ToListAsync();
  }

  // User Participation Management
  public async Task<ProgramUser> AddUserAsync(Guid programId, Guid userId) {
    var existingUser = await context.ProgramUsers
                                    .Where(pu => !pu.IsDeleted && pu.ProgramId == programId && pu.UserId == userId)
                                    .FirstOrDefaultAsync();

    if (existingUser != null) {
      if (!existingUser.IsActive) {
        existingUser.IsActive = true;
        existingUser.JoinedAt = DateTime.UtcNow;
        existingUser.Touch();
        await context.SaveChangesAsync();
      }

      return existingUser;
    }

    var programUser = new ProgramUser { ProgramId = programId, UserId = userId, IsActive = true, JoinedAt = DateTime.UtcNow };

    context.ProgramUsers.Add(programUser);
    await context.SaveChangesAsync();

    return programUser;
  }

  public async Task<ProgramUser> RemoveUserAsync(Guid programId, Guid userId) {
    var programUser = await context.ProgramUsers
                                   .Where(pu => !pu.IsDeleted && pu.ProgramId == programId && pu.UserId == userId)
                                   .FirstOrDefaultAsync();

    if (programUser != null) {
      programUser.IsActive = false;
      programUser.Touch();
      await context.SaveChangesAsync();
    }

    return programUser!;
  }

  public async Task<IEnumerable<ProgramUser>> GetProgramUsersAsync(Guid programId) {
    return await context.ProgramUsers.Include(pu => pu.User)
                        .Where(pu => !pu.IsDeleted && pu.ProgramId == programId && pu.IsActive)
                        .OrderBy(pu => pu.JoinedAt)
                        .ToListAsync();
  }

  public async Task<IEnumerable<ProgramEntity>> GetUserProgramsAsync(Guid userId) {
    return await context.ProgramUsers.Include(pu => pu.Program)
                        .Where(pu => !pu.IsDeleted && pu.UserId == userId && pu.IsActive)
                        .Select(pu => pu.Program)
                        .Where(p => !p.IsDeleted)
                        .OrderByDescending(p => p.CreatedAt)
                        .ToListAsync();
  }

  public async Task<bool> IsUserInProgramAsync(Guid programId, Guid userId) {
    return await context.ProgramUsers
                        .Where(pu => !pu.IsDeleted && pu.ProgramId == programId && pu.UserId == userId && pu.IsActive)
                        .AnyAsync();
  }

  // Progress & Analytics
  public async Task<decimal> GetUserProgressAsync(Guid programId, Guid userId) {
    var programUser = await context.ProgramUsers
                                   .Where(pu => !pu.IsDeleted && pu.ProgramId == programId && pu.UserId == userId)
                                   .FirstOrDefaultAsync();

    return programUser?.CompletionPercentage ?? 0;
  }

  public async Task<IEnumerable<ContentInteraction>> GetUserInteractionsAsync(Guid programId, Guid userId) {
    var programUser = await context.ProgramUsers
                                   .Where(pu => !pu.IsDeleted && pu.ProgramId == programId && pu.UserId == userId)
                                   .FirstOrDefaultAsync();

    if (programUser == null) return Enumerable.Empty<ContentInteraction>();

    return await context.ContentInteractions.Include(ci => ci.Content)
                        .Where(ci => !ci.IsDeleted && ci.ProgramUserId == programUser.Id)
                        .OrderBy(ci => ci.Content.SortOrder)
                        .ToListAsync();
  }

  public async Task<ProgramEntity> UpdateUserProgressAsync(
    Guid programId, Guid userId, Guid contentId,
    ProgressStatus status
  ) {
    var program = await GetProgramByIdAsync(programId);

    if (program == null) throw new ArgumentException("Program not found", nameof(programId));

    var programUser = await context.ProgramUsers
                                   .Where(pu => !pu.IsDeleted && pu.ProgramId == programId && pu.UserId == userId)
                                   .FirstOrDefaultAsync();

    if (programUser == null) throw new ArgumentException("User not enrolled in program");

    var interaction = await context.ContentInteractions
                                   .Where(ci => !ci.IsDeleted && ci.ProgramUserId == programUser.Id && ci.ContentId == contentId)
                                   .FirstOrDefaultAsync();

    if (interaction == null) {
      interaction = new ContentInteraction {
        ProgramUserId = programUser.Id,
        ContentId = contentId,
        Status = status,
        FirstAccessedAt = DateTime.UtcNow,
        LastAccessedAt = DateTime.UtcNow,
      };

      context.ContentInteractions.Add(interaction);
    }
    else {
      interaction.Status = status;
      interaction.LastAccessedAt = DateTime.UtcNow;

      if (status == ProgressStatus.Completed && interaction.CompletedAt == null) {
        interaction.CompletedAt = DateTime.UtcNow;
        interaction.CompletionPercentage = 100;
      }

      interaction.Touch();
    }

    // Recalculate overall progress
    await RecalculateUserProgressAsync(programUser.Id);

    await context.SaveChangesAsync();

    return program;
  }

  // Lifecycle Management
  public async Task<ProgramEntity> CreateDraftAsync(ProgramEntity program) {
    program.Status = ContentStatus.Draft;

    return await CreateProgramAsync(program);
  }

  public async Task<ProgramEntity> SubmitForReviewAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) throw new ArgumentException("Program not found", nameof(id));

    program.Status = ContentStatus.UnderReview;
    program.Touch();
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity> ApproveAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) throw new ArgumentException("Program not found", nameof(id));

    program.Status = ContentStatus.Published;
    program.Touch();
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity> RejectAsync(Guid id, string reason) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) throw new ArgumentException("Program not found", nameof(id));

    program.Status = ContentStatus.Draft;
    // Store rejection reason in metadata
    program.SetMetadata("rejectionReason", reason);
    program.SetMetadata("rejectionDate", DateTime.UtcNow);
    program.Touch();
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity> ArchiveAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) throw new ArgumentException("Program not found", nameof(id));

    program.Status = ContentStatus.Archived;
    program.Touch();
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity> RestoreAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) throw new ArgumentException("Program not found", nameof(id));

    program.Status = ContentStatus.Draft;
    program.Touch();
    await context.SaveChangesAsync();

    return program;
  }

  // Publishing Operations
  public async Task<ProgramEntity> PublishProgramAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) throw new ArgumentException("Program not found", nameof(id));

    program.Status = ContentStatus.Published;
    program.Touch();
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity> UnpublishProgramAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) throw new ArgumentException("Program not found", nameof(id));

    program.Status = ContentStatus.Draft;
    program.Touch();
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity> SchedulePublishAsync(Guid id, DateTime publishAt) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) throw new ArgumentException("Program not found", nameof(id));

    program.SetMetadata("scheduledPublishAt", publishAt);
    program.Touch();
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity> SetVisibilityAsync(Guid id, AccessLevel visibility) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) throw new ArgumentException("Program not found", nameof(id));

    program.Visibility = visibility;
    program.Touch();
    await context.SaveChangesAsync();

    return program;
  }

  // Search & Discovery
  public async Task<IEnumerable<ProgramEntity>> SearchProgramsAsync(string searchTerm, int skip = 0, int take = 50) {
    if (string.IsNullOrWhiteSpace(searchTerm)) return await GetProgramsAsync(skip, take);

    return await context.Programs
                        .Where(p => !p.IsDeleted &&
                                    (p.Title.Contains(searchTerm) || (p.Description != null && p.Description.Contains(searchTerm)))
                        )
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync();
  }

  public async Task<IEnumerable<ProgramEntity>> GetProgramsByCreatorAsync(Guid creatorId, int skip = 0, int take = 50) {
    // Since Program doesn't have CreatorId, we'll return all programs for now
    // In a real implementation, you'd need to add a CreatorId property to Program
    return await context.Programs.Where(p => !p.IsDeleted)
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync();
  }

  public async Task<IEnumerable<ProgramEntity>> GetFeaturedProgramsAsync(int count = 10) {
    return await context.Programs.Where(p => !p.IsDeleted && p.Status == ContentStatus.Published)
                        .OrderByDescending(p => p.ProgramUsers.Count(pu => !pu.IsDeleted && pu.IsActive))
                        .Take(count)
                        .ToListAsync();
  }

  public async Task<IEnumerable<ProgramEntity>> GetRecentProgramsAsync(int count = 10) {
    return await context.Programs.Where(p => !p.IsDeleted && p.Status == ContentStatus.Published)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(count)
                        .ToListAsync();
  }

  public async Task<IEnumerable<ProgramEntity>> GetPopularProgramsAsync(int count = 10) {
    return await context.Programs.Where(p => !p.IsDeleted && p.Status == ContentStatus.Published)
                        .OrderByDescending(p => p.ProgramUsers.Count(pu => !pu.IsDeleted && pu.IsActive))
                        .ThenByDescending(p => p.CreatedAt)
                        .Take(count)
                        .ToListAsync();
  }

  // Analytics & Statistics
  public async Task<int> GetProgramCountAsync(ContentStatus? status = null, AccessLevel? visibility = null) {
    var query = context.Programs.Where(p => !p.IsDeleted);

    if (status.HasValue) query = query.Where(p => p.Status == status.Value);

    if (visibility.HasValue) query = query.Where(p => p.Visibility == visibility.Value);

    return await query.CountAsync();
  }

  public async Task<int> GetUserCountForProgramAsync(Guid programId) {
    return await context.ProgramUsers.Where(pu => !pu.IsDeleted && pu.ProgramId == programId && pu.IsActive)
                        .CountAsync();
  }

  public async Task<decimal> GetAverageCompletionRateAsync(Guid programId) {
    var averageCompletion = await context.ProgramUsers
                                         .Where(pu => !pu.IsDeleted && pu.ProgramId == programId && pu.IsActive)
                                         .AverageAsync(pu => (decimal?)pu.CompletionPercentage) ??
                            0;

    return averageCompletion;
  }

  public async Task<Dictionary<string, object>> GetProgramStatisticsAsync(Guid programId) {
    var userCount = await GetUserCountForProgramAsync(programId);
    var averageCompletion = await GetAverageCompletionRateAsync(programId);
    var completedCount = await context.ProgramUsers
                                      .Where(pu => !pu.IsDeleted && pu.ProgramId == programId && pu.IsActive && pu.CompletedAt != null)
                                      .CountAsync();

    return new Dictionary<string, object> { ["totalUsers"] = userCount, ["averageCompletion"] = averageCompletion, ["completedUsers"] = completedCount, ["completionRate"] = userCount > 0 ? (decimal)completedCount / userCount * 100 : 0 };
  }

  // Private Helper Methods
  private static string GenerateSlug(string title) { return title.ToLowerInvariant().Replace(" ", "-").Replace("'", "").Replace("\"", ""); }

  private async Task RecalculateUserProgressAsync(Guid programUserId) {
    var programUser = await context.ProgramUsers.Where(pu => pu.Id == programUserId).FirstOrDefaultAsync();

    if (programUser == null) return;

    var totalContent = await context.ProgramContents
                                    .Where(pc => !pc.IsDeleted && pc.ProgramId == programUser.ProgramId && pc.IsRequired)
                                    .CountAsync();

    if (totalContent == 0) {
      programUser.CompletionPercentage = 0;

      return;
    }

    var completedContent = await context.ContentInteractions.Where(ci =>
                                                                     !ci.IsDeleted && ci.ProgramUserId == programUserId && ci.Status == ProgressStatus.Completed
                                        )
                                        .CountAsync();

    programUser.CompletionPercentage = (decimal)completedContent / totalContent * 100;

    if (programUser is { CompletionPercentage: >= 100, CompletedAt: null }) programUser.CompletedAt = DateTime.UtcNow;

    programUser.Touch();
  }

  // Additional methods required by the controller

  // CRUD Operations with DTOs
  public async Task<ProgramEntity> CreateProgramAsync(CreateProgramDto createDto) {
    var program = new ProgramEntity {
      Id = Guid.NewGuid(),
      Title = createDto.Title,
      Description = createDto.Description,
      Slug = createDto.Slug,
      Thumbnail = createDto.Thumbnail,
      Status = ContentStatus.Draft,
      Visibility = AccessLevel.Private,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    context.Programs.Add(program);
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity?> UpdateProgramAsync(Guid id, UpdateProgramDto updateDto) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    if (updateDto.Title != null) program.Title = updateDto.Title;
    if (updateDto.Description != null) program.Description = updateDto.Description;
    if (updateDto.Thumbnail != null) program.Thumbnail = updateDto.Thumbnail;

    program.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return program;
  }

  // Category and Difficulty Operations
  public async Task<IEnumerable<ProgramEntity>> GetProgramsByCategoryAsync(
    ProgramCategory category, int skip = 0,
    int take = 50
  ) {
    return await context.Programs.Where(p => !p.IsDeleted && p.Category == category)
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync();
  }

  public async Task<IEnumerable<ProgramEntity>> GetProgramsByDifficultyAsync(
    ProgramDifficulty difficulty, int skip = 0,
    int take = 50
  ) {
    return await context.Programs.Where(p => !p.IsDeleted && p.Difficulty == difficulty)
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync();
  }

  public async Task<IEnumerable<ProgramEntity>> GetPublishedProgramsAsync(int skip = 0, int take = 50) {
    return await context.Programs.Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published)
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync();
  }

  // Content Management with DTOs
  public async Task<ProgramContent?> AddContentAsync(Guid programId, CreateContentDto contentDto) {
    var program = await GetProgramByIdAsync(programId);

    if (program == null) return null;

    var content = new ProgramContent {
      Id = Guid.NewGuid(),
      ProgramId = programId,
      Title = contentDto.Title,
      Description = contentDto.Description,
      Type = contentDto.Type,
      Body = contentDto.Body,
      SortOrder = contentDto.SortOrder ?? 0,
      IsRequired = contentDto.IsRequired,
      EstimatedMinutes = contentDto.EstimatedMinutes,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    context.ProgramContents.Add(content);
    await context.SaveChangesAsync();

    return content;
  }

  public async Task<ProgramContent?> UpdateContentAsync(Guid programId, Guid contentId, UpdateContentDto contentDto) {
    var content =
      await context.ProgramContents.FirstOrDefaultAsync(c =>
                                                          c.Id == contentId && c.ProgramId == programId && !c.IsDeleted
      );

    if (content == null) return null;

    if (contentDto.Title != null) content.Title = contentDto.Title;
    if (contentDto.Description != null) content.Description = contentDto.Description;
    if (contentDto.Body != null) content.Body = contentDto.Body;
    if (contentDto.SortOrder != null) content.SortOrder = contentDto.SortOrder.Value;
    if (contentDto.IsRequired != null) content.IsRequired = contentDto.IsRequired.Value;
    if (contentDto.EstimatedMinutes != null) content.EstimatedMinutes = contentDto.EstimatedMinutes;

    content.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return content;
  }

  public async Task<bool> RemoveContentAsync(Guid programId, Guid contentId) {
    var content =
      await context.ProgramContents.FirstOrDefaultAsync(c =>
                                                          c.Id == contentId && c.ProgramId == programId && !c.IsDeleted
      );

    if (content == null) return false;

    content.SoftDelete();
    await context.SaveChangesAsync();

    return true;
  }

  // User Management
  public async Task<UserProgressDto?> AddUserToProgramAsync(Guid programId, Guid userId) {
    var program = await GetProgramByIdAsync(programId);

    if (program == null) return null;

    var existingUser =
      await context.ProgramUsers.FirstOrDefaultAsync(pu =>
                                                       pu.ProgramId == programId && pu.UserId == userId && !pu.IsDeleted
      );

    if (existingUser != null)
      // User already exists, return their progress
      return await GetUserProgressDtoAsync(programId, userId);

    var programUser = new ProgramUser {
      Id = Guid.NewGuid(),
      ProgramId = programId,
      UserId = userId,
      JoinedAt = DateTime.UtcNow,
      LastAccessedAt = DateTime.UtcNow,
      CompletionPercentage = 0,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    context.ProgramUsers.Add(programUser);
    await context.SaveChangesAsync();

    return await GetUserProgressDtoAsync(programId, userId);
  }

  public async Task<bool> RemoveUserFromProgramAsync(Guid programId, Guid userId) {
    var programUser =
      await context.ProgramUsers.FirstOrDefaultAsync(pu =>
                                                       pu.ProgramId == programId && pu.UserId == userId && !pu.IsDeleted
      );

    if (programUser == null) return false;

    programUser.SoftDelete();
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<IEnumerable<UserProgressDto>> GetProgramUsersAsync(Guid programId, int skip = 0, int take = 50) {
    var programUsers = await context.ProgramUsers.Where(pu => pu.ProgramId == programId && !pu.IsDeleted)
                                    .Skip(skip)
                                    .Take(take)
                                    .ToListAsync();

    var result = new List<UserProgressDto>();

    foreach (var pu in programUsers) {
      var progress = await GetUserProgressDtoAsync(programId, pu.UserId);
      if (progress != null) result.Add(progress);
    }

    return result;
  }

  public async Task<UserProgressDto?> GetUserProgressDtoAsync(Guid programId, Guid userId) {
    var programUser =
      await context.ProgramUsers.FirstOrDefaultAsync(pu =>
                                                       pu.ProgramId == programId && pu.UserId == userId && !pu.IsDeleted
      );

    if (programUser == null) return null;

    // Get content progress (stub implementation)
    var contentProgress = new List<ContentProgressDto>();

    return new UserProgressDto(
      programUser.CompletionPercentage,
      programUser.LastAccessedAt,
      programUser.StartedAt, // Using StartedAt instead of CreatedAt
      programUser.CompletedAt,
      contentProgress
    );
  }

  public async Task<UserProgressDto?> UpdateUserProgressAsync(
    Guid programId, Guid userId,
    UpdateProgressDto progressDto
  ) {
    var programUser =
      await context.ProgramUsers.FirstOrDefaultAsync(pu =>
                                                       pu.ProgramId == programId && pu.UserId == userId && !pu.IsDeleted
      );

    if (programUser == null) return null;

    if (progressDto.LastAccessedAt != null) programUser.LastAccessedAt = progressDto.LastAccessedAt.Value;
    programUser.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return await GetUserProgressDtoAsync(programId, userId);
  }

  public async Task<bool> MarkContentCompletedAsync(Guid programId, Guid userId, Guid contentId) {
    // Get the program user to recalculate progress
    var programUser =
      await context.ProgramUsers.FirstOrDefaultAsync(pu =>
                                                       pu.ProgramId == programId && pu.UserId == userId && !pu.IsDeleted
      );

    if (programUser == null) return false;

    await RecalculateUserProgressAsync(programUser.Id);

    return true;
  }

  public async Task<bool> ResetUserProgressAsync(Guid programId, Guid userId) {
    var programUser =
      await context.ProgramUsers.FirstOrDefaultAsync(pu =>
                                                       pu.ProgramId == programId && pu.UserId == userId && !pu.IsDeleted
      );

    if (programUser == null) return false;

    // Reset user progress
    programUser.CompletionPercentage = 0;
    programUser.CompletedAt = null;
    programUser.LastAccessedAt = DateTime.UtcNow;
    programUser.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return true;
  }

  // Lifecycle Management - Stub implementations
  public async Task<ProgramEntity?> SubmitProgramAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    program.Status = ContentStatus.Published; // Using Published as substitute for submitted
    program.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity?> ApproveProgramAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    program.Status = ContentStatus.Published; // Using Published as approved
    program.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity?> RejectProgramAsync(Guid id, string reason) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    program.Status = ContentStatus.Draft; // Using Draft as rejected
    program.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity?> WithdrawProgramAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    program.Status = ContentStatus.Draft;
    program.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity?> ArchiveProgramAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    program.Status = ContentStatus.Archived;
    program.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity?> RestoreProgramAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    program.Status = ContentStatus.Published;
    program.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity?> ScheduleProgramAsync(Guid id, DateTime publishAt) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    // In a real implementation, you'd schedule the publication
    // For now, just set the status to published
    program.Status = ContentStatus.Published;
    program.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return program;
  }

  // Monetization - Stub implementations
  public async Task<ProgramEntity?> EnableMonetizationAsync(Guid id, MonetizationDto monetizationDto) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    // In a real implementation, you'd set monetization properties
    program.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<ProgramEntity?> DisableMonetizationAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    // In a real implementation, you'd disable monetization
    program.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return program;
  }

  public async Task<PricingDto?> GetProgramPricingAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    // Stub implementation
    return new PricingDto(0, "USD", false, null, false);
  }

  public async Task<PricingDto?> UpdateProgramPricingAsync(Guid id, UpdatePricingDto pricingDto) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    // In a real implementation, you'd update pricing
    return await GetProgramPricingAsync(id);
  }

  // Analytics - Stub implementations
  public async Task<ProgramAnalyticsDto?> GetProgramAnalyticsAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    var userCount = await context.ProgramUsers.CountAsync(pu => pu.ProgramId == id && !pu.IsDeleted);

    return new ProgramAnalyticsDto(
      id,
      program.Title,
      userCount,
      userCount, // Stub: active users = total users
      0, // Stub: completed users
      0, // Stub: completion rate
      TimeSpan.Zero, // Stub: average completion time
      0, // Stub: total views
      DateTime.UtcNow, // Stub: last activity
      new Dictionary<string, object>() // Stub: additional metrics
    );
  }

  public async Task<CompletionRatesDto?> GetCompletionRatesAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    // Stub implementation
    return new CompletionRatesDto(id, 0, new Dictionary<Guid, decimal>(), new List<CompletionTrendDto>());
  }

  public async Task<EngagementMetricsDto?> GetEngagementMetricsAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    // Stub implementation
    return new EngagementMetricsDto(
      id,
      0,
      0,
      0, // DAU, WAU, MAU
      TimeSpan.Zero,
      0,
      0,
      new Dictionary<string, int>()
    );
  }

  public async Task<RevenueAnalyticsDto?> GetRevenueAnalyticsAsync(Guid id) {
    var program = await GetProgramByIdAsync(id);

    if (program == null) return null;

    // Stub implementation
    return new RevenueAnalyticsDto(
      id,
      0,
      0, // Total and monthly revenue
      0,
      0, // Total and monthly purchases
      0,
      0, // ARPU and conversion rate
      new List<RevenueChartDto>()
    );
  }

  // Product Integration - Stub implementations
  public async Task<Guid?> CreateProductFromProgramAsync(Guid programId, CreateProductFromProgramDto productDto) {
    var program = await GetProgramByIdAsync(programId);

    if (program == null) return null;

    // In a real implementation, you'd create a product in the product service
    // For now, return a stub product ID
    return Guid.NewGuid();
  }

  public async Task<bool> LinkProgramToProductAsync(Guid programId, Guid productId) {
    var program = await GetProgramByIdAsync(programId);

    if (program == null) return false;

    // In a real implementation, you'd create a link in a junction table
    return true;
  }

  public async Task<bool> UnlinkProgramFromProductAsync(Guid programId, Guid productId) {
    var program = await GetProgramByIdAsync(programId);

    if (program == null) return false;

    // In a real implementation, you'd remove the link
    return true;
  }

  public async Task<IEnumerable<Guid>> GetLinkedProductsAsync(Guid programId) {
    var program = await GetProgramByIdAsync(programId);

    if (program == null) return new List<Guid>();

    // In a real implementation, you'd query the junction table
    return new List<Guid>();
  }
}
