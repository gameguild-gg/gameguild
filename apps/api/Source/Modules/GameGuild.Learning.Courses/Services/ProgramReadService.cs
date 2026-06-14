using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Read-side service for Programs: lookups, listings, search, filtering,
/// analytics, statistics, and read-only user/progress queries.
/// </summary>
public class ProgramReadService(IApplicationDbContext context) : IProgramReadService
{
  // ── Single-entity Lookups ───────────────────────────────────────────

  public async Task<Program?> GetProgramByIdAsync(Guid id) { return await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == id); }

  public async Task<Program?> GetProgramBySlugAsync(string slug) { return await context.Set<Program>().Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Slug == slug); }

  public async Task<Program?> GetPublishedProgramBySlugAsync(string slug)
  {
    return await context.Set<Program>().Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published && p.Visibility == ContentVisibility.Public).FirstOrDefaultAsync(p => p.Slug == slug);
  }

  public async Task<Program?> GetProgramWithContentAsync(Guid id)
  {
    return await context.Set<Program>().Include(p => p.ProgramContents.Where(pc => pc.DeletedAt == null)).Include(p => p.ProgramUsers.Where(pu => pu.DeletedAt == null)).Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == id);
  }

  public async Task<bool> ProgramExistsAsync(Guid id) { return await context.Set<Program>().Where(p => p.DeletedAt == null).AnyAsync(p => p.Id == id); }

  // ── Listings ────────────────────────────────────────────────────────

  public async Task<IEnumerable<Program>> GetProgramsAsync(int skip = 0, int take = 50) { return await context.Set<Program>().Where(p => p.DeletedAt == null).OrderByDescending(p => p.CreatedAt).Skip(skip).Take(take).ToListAsync(); }

  public async Task<IEnumerable<ProgramContent>> GetProgramContentAsync(Guid programId) { return await context.Set<ProgramContent>().Where(pc => pc.DeletedAt == null && pc.ProgramId == programId).OrderBy(pc => pc.SortOrder).ToListAsync(); }

  public async Task<IEnumerable<Program>> GetPublishedProgramsAsync(int skip = 0, int take = 50)
  {
    return await context.Set<Program>().Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published).OrderByDescending(p => p.CreatedAt).Skip(skip).Take(take).ToListAsync();
  }

  public async Task<IEnumerable<Program>> GetPublicPublishedProgramsAsync(int skip = 0, int take = 50)
  {
    return await context.Set<Program>()
      .Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published && p.Visibility == ContentVisibility.Public)
      .OrderByDescending(p => p.CreatedAt)
      .Skip(skip)
      .Take(take)
      .ToListAsync();
  }

  // ── Search & Discovery ──────────────────────────────────────────────

  public async Task<IEnumerable<Program>> SearchProgramsAsync(string searchTerm, int skip = 0, int take = 50)
  {
    if (string.IsNullOrWhiteSpace(searchTerm)) return await GetProgramsAsync(skip, take).ConfigureAwait(false);

    return await context.Set<Program>().Where(p => p.DeletedAt == null && (p.Title.Contains(searchTerm) || (p.Description != null && p.Description.Contains(searchTerm)))).OrderByDescending(p => p.CreatedAt).Skip(skip).Take(take).ToListAsync();
  }

  public async Task<IEnumerable<Program>> GetProgramsByCreatorAsync(Guid creatorId, int skip = 0, int take = 50)
  {
    return await context.Set<Program>().Where(p => p.DeletedAt == null).OrderByDescending(p => p.CreatedAt).Skip(skip).Take(take).ToListAsync();
  }

  public async Task<IEnumerable<Program>> GetFeaturedProgramsAsync(int count = 10)
  {
    return await context.Set<Program>().Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published).OrderByDescending(p => p.ProgramUsers.Count(pu => pu.DeletedAt == null && pu.IsActive)).Take(count).ToListAsync();
  }

  public async Task<IEnumerable<Program>> GetRecentProgramsAsync(int count = 10)
  {
    return await context.Set<Program>().Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published).OrderByDescending(p => p.CreatedAt).Take(count).ToListAsync();
  }

  public async Task<IEnumerable<Program>> GetPopularProgramsAsync(int count = 10)
  {
    return await context.Set<Program>().Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published).OrderByDescending(p => p.ProgramUsers.Count(pu => pu.DeletedAt == null && pu.IsActive)).ThenByDescending(p => p.CreatedAt).Take(count).ToListAsync();
  }

  public async Task<IEnumerable<Program>> GetProgramsByCategoryAsync(ProgramCategory category, int skip = 0, int take = 50)
  {
    return await context.Set<Program>().Where(p => p.DeletedAt == null && p.Category == category).OrderByDescending(p => p.CreatedAt).Skip(skip).Take(take).ToListAsync();
  }

  public async Task<IEnumerable<Program>> GetProgramsByDifficultyAsync(ProgramDifficulty difficulty, int skip = 0, int take = 50)
  {
    return await context.Set<Program>().Where(p => p.DeletedAt == null && p.Difficulty == difficulty).OrderByDescending(p => p.CreatedAt).Skip(skip).Take(take).ToListAsync();
  }

  // ── User & Progress Queries ─────────────────────────────────────────

  public async Task<IEnumerable<ProgramUser>> GetProgramUsersAsync(Guid programId)
  {
    return await context.Set<ProgramUser>().Include(pu => pu.User).Where(pu => pu.DeletedAt == null && pu.ProgramId == programId && pu.IsActive).OrderBy(pu => pu.JoinedAt).ToListAsync();
  }

  public async Task<IEnumerable<Program>> GetUserProgramsAsync(Guid userId)
  {
    return await context.Set<ProgramUser>().Include(pu => pu.Program).Where(pu => pu.DeletedAt == null && pu.UserId == userId && pu.IsActive).Select(pu => pu.Program).Where(p => p.DeletedAt == null).OrderByDescending(p => p.CreatedAt).ToListAsync();
  }

  public async Task<bool> IsUserInProgramAsync(Guid programId, Guid userId) { return await context.Set<ProgramUser>().Where(pu => pu.DeletedAt == null && pu.ProgramId == programId && pu.UserId == userId && pu.IsActive).AnyAsync(); }

  public async Task<IEnumerable<UserProgressDto>> GetProgramUsersAsync(Guid programId, int skip = 0, int take = 50)
  {
    var programUsers = await context.Set<ProgramUser>().Where(pu => pu.ProgramId == programId && pu.DeletedAt == null).Skip(skip).Take(take).ToListAsync();

    var result = new List<UserProgressDto>();

    foreach (var pu in programUsers)
    {
      var progress = await GetUserProgressDtoAsync(programId, pu.UserId).ConfigureAwait(false);
      if (progress != null) result.Add(progress);
    }

    return result;
  }

  public async Task<decimal> GetUserProgressAsync(Guid programId, Guid userId)
  {
    var programUser = await context.Set<ProgramUser>().Where(pu => pu.DeletedAt == null && pu.ProgramId == programId && pu.UserId == userId).FirstOrDefaultAsync();

    return programUser?.CompletionPercentage ?? 0;
  }

  public async Task<UserProgressDto?> GetUserProgressDtoAsync(Guid programId, Guid userId)
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

  public async Task<IEnumerable<ContentInteraction>> GetUserInteractionsAsync(Guid programId, Guid userId)
  {
    var programUser = await context.Set<ProgramUser>().Where(pu => pu.DeletedAt == null && pu.ProgramId == programId && pu.UserId == userId).FirstOrDefaultAsync();

    if (programUser == null) return [];

    return await context.Set<ContentInteraction>().Include(ci => ci.Content).Where(ci => ci.DeletedAt == null && ci.ProgramUserId == programUser.Id).OrderBy(ci => ci.Content.SortOrder).ToListAsync();
  }

  // ── Analytics & Statistics ──────────────────────────────────────────

  public async Task<int> GetProgramCountAsync(ContentStatus? status = null, ContentVisibility? visibility = null)
  {
    var query = context.Set<Program>().Where(p => p.DeletedAt == null);

    if (status.HasValue) query = query.Where(p => p.Status == status.Value);

    if (visibility.HasValue) query = query.Where(p => p.Visibility == visibility.Value);

    return await query.CountAsync().ConfigureAwait(false);
  }

  public async Task<int> GetUserCountForProgramAsync(Guid programId) { return await context.Set<ProgramUser>().Where(pu => pu.DeletedAt == null && pu.ProgramId == programId && pu.IsActive).CountAsync(); }

  public async Task<decimal> GetAverageCompletionRateAsync(Guid programId)
  {
    var averageCompletion = await context.Set<ProgramUser>().Where(pu => pu.DeletedAt == null && pu.ProgramId == programId && pu.IsActive).AverageAsync(pu => (decimal?)pu.CompletionPercentage) ?? 0;

    return averageCompletion;
  }

  public async Task<Dictionary<string, object>> GetProgramStatisticsAsync(Guid programId)
  {
    var userCount = await GetUserCountForProgramAsync(programId).ConfigureAwait(false);
    var averageCompletion = await GetAverageCompletionRateAsync(programId).ConfigureAwait(false);
    var completedCount = await context.Set<ProgramUser>().Where(pu => pu.DeletedAt == null && pu.ProgramId == programId && pu.IsActive && pu.CompletedAt != null).CountAsync();

    return new Dictionary<string, object> { ["totalUsers"] = userCount, ["averageCompletion"] = averageCompletion, ["completedUsers"] = completedCount, ["completionRate"] = userCount > 0 ? (decimal)completedCount / userCount * 100 : 0 };
  }

  public async Task<ProgramAnalyticsDto?> GetProgramAnalyticsAsync(Guid id)
  {
    var program = await GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null) return null;

    var userCount = await context.Set<ProgramUser>().CountAsync(pu => pu.ProgramId == id && pu.DeletedAt == null);

    return new ProgramAnalyticsDto(
      id,
      program.Title,
      userCount,
      userCount,
      0,
      0,
      TimeSpan.Zero,
      0,
      SystemClock.UtcNow,
      new Dictionary<string, object>()
    );
  }

  public async Task<CompletionRatesDto?> GetCompletionRatesAsync(Guid id)
  {
    var program = await GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null) return null;

    return new CompletionRatesDto(id, 0, new Dictionary<Guid, decimal>(), []);
  }

  public async Task<EngagementMetricsDto?> GetEngagementMetricsAsync(Guid id)
  {
    var program = await GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null) return null;

    return new EngagementMetricsDto(
      id,
      0,
      0,
      0,
      TimeSpan.Zero,
      0,
      0,
      new Dictionary<string, int>()
    );
  }

  public async Task<RevenueAnalyticsDto?> GetRevenueAnalyticsAsync(Guid id)
  {
    var program = await GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null) return null;

    return new RevenueAnalyticsDto(
      id,
      0,
      0,
      0,
      0,
      0,
      0,
      []
    );
  }

  // ── Pricing & Product Queries ───────────────────────────────────────

  public async Task<PricingDto?> GetProgramPricingAsync(Guid id)
  {
    var program = await GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null) return null;

    return ProgramPricingMetadata.Read(program);
  }

  public async Task<IEnumerable<Guid>> GetLinkedProductsAsync(Guid programId)
  {
    var program = await GetProgramByIdAsync(programId).ConfigureAwait(false);

    if (program == null) return new List<Guid>();

    return new List<Guid>();
  }
}
