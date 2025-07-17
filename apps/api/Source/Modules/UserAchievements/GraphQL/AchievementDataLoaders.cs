using GameGuild.Database;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// DataLoader for Achievement entities
/// </summary>
public interface IAchievementDataLoader : IDataLoader<Guid, Achievement?> { }

public class AchievementDataLoader : BatchDataLoader<Guid, Achievement?>, IAchievementDataLoader {
  private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

  public AchievementDataLoader(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IBatchScheduler batchScheduler,
    DataLoaderOptions? options = null)
    : base(batchScheduler, options ?? new DataLoaderOptions()) {
    _dbContextFactory = dbContextFactory;
  }

  protected override async Task<IReadOnlyDictionary<Guid, Achievement?>> LoadBatchAsync(
    IReadOnlyList<Guid> keys,
    CancellationToken cancellationToken) {
    await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

    var achievements = await context.Achievements
      .Where(a => keys.Contains(a.Id))
      .ToListAsync(cancellationToken);

    return achievements.ToDictionary(a => a.Id, a => (Achievement?)a);
  }
}

/// <summary>
/// DataLoader for UserAchievement entities by Achievement ID
/// </summary>
public interface IUserAchievementDataLoader : IDataLoader<Guid, IEnumerable<UserAchievement>> { }

public class UserAchievementDataLoader : BatchDataLoader<Guid, IEnumerable<UserAchievement>>, IUserAchievementDataLoader {
  private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

  public UserAchievementDataLoader(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IBatchScheduler batchScheduler,
    DataLoaderOptions? options = null)
    : base(batchScheduler, options ?? new DataLoaderOptions()) {
    _dbContextFactory = dbContextFactory;
  }

  protected override async Task<IReadOnlyDictionary<Guid, IEnumerable<UserAchievement>>> LoadBatchAsync(
    IReadOnlyList<Guid> keys,
    CancellationToken cancellationToken) {
    await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

    var userAchievements = await context.UserAchievements
      .Where(ua => keys.Contains(ua.AchievementId))
      .ToListAsync(cancellationToken);

    return userAchievements
      .GroupBy(ua => ua.AchievementId)
      .ToDictionary(g => g.Key, g => g.AsEnumerable());
  }
}

/// <summary>
/// DataLoader for AchievementLevel entities by Achievement ID
/// </summary>
public interface IAchievementLevelDataLoader : IDataLoader<Guid, AchievementLevel[]> { }

public class AchievementLevelDataLoader : GroupedDataLoader<Guid, AchievementLevel>, IAchievementLevelDataLoader {
  private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

  public AchievementLevelDataLoader(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IBatchScheduler batchScheduler,
    DataLoaderOptions? options = null)
    : base(batchScheduler, options) {
    _dbContextFactory = dbContextFactory;
  }

  protected override async Task<ILookup<Guid, AchievementLevel>> LoadGroupedBatchAsync(
    IReadOnlyList<Guid> keys,
    CancellationToken cancellationToken) {
    await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

    var achievementLevels = await context.AchievementLevels
      .Where(al => keys.Contains(al.AchievementId))
      .OrderBy(al => al.Level)
      .ToListAsync(cancellationToken);

    return achievementLevels.ToLookup(al => al.AchievementId);
  }
}

/// <summary>
/// DataLoader for AchievementPrerequisite entities by Achievement ID
/// </summary>
public interface IAchievementPrerequisiteDataLoader : IDataLoader<Guid, AchievementPrerequisite[]> { }

public class AchievementPrerequisiteDataLoader : GroupedDataLoader<Guid, AchievementPrerequisite>, IAchievementPrerequisiteDataLoader {
  private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

  public AchievementPrerequisiteDataLoader(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IBatchScheduler batchScheduler,
    DataLoaderOptions? options = null)
    : base(batchScheduler, options) {
    _dbContextFactory = dbContextFactory;
  }

  protected override async Task<ILookup<Guid, AchievementPrerequisite>> LoadGroupedBatchAsync(
    IReadOnlyList<Guid> keys,
    CancellationToken cancellationToken) {
    await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

    var prerequisites = await context.AchievementPrerequisites
      .Include(ap => ap.PrerequisiteAchievement)
      .Where(ap => keys.Contains(ap.AchievementId))
      .ToListAsync(cancellationToken);

    return prerequisites.ToLookup(ap => ap.AchievementId);
  }
}

/// <summary>
/// DataLoader for Achievement Statistics
/// </summary>
public interface IAchievementStatisticsDataLoader : IDataLoader<Guid, AchievementStatisticsDto> { }

public class AchievementStatisticsDataLoader : BatchDataLoader<Guid, AchievementStatisticsDto>, IAchievementStatisticsDataLoader {
  private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

  public AchievementStatisticsDataLoader(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IBatchScheduler batchScheduler,
    DataLoaderOptions? options = null)
    : base(batchScheduler, options ?? new DataLoaderOptions()) {
    _dbContextFactory = dbContextFactory;
  }

  protected override async Task<IReadOnlyDictionary<Guid, AchievementStatisticsDto>> LoadBatchAsync(
    IReadOnlyList<Guid> keys,
    CancellationToken cancellationToken) {
    await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

    var results = new Dictionary<Guid, AchievementStatisticsDto>();

    foreach (var achievementId in keys) {
      var earnCount = await context.UserAchievements
        .CountAsync(ua => ua.AchievementId == achievementId, cancellationToken);

      var uniqueUsers = await context.UserAchievements
        .Where(ua => ua.AchievementId == achievementId)
        .Select(ua => ua.UserId)
        .Distinct()
        .CountAsync(cancellationToken);

      var totalUsers = await context.Users.CountAsync(cancellationToken);
      var earnRate = totalUsers > 0 ? (double)uniqueUsers / totalUsers * 100 : 0;

      results[achievementId] = new AchievementStatisticsDto {
        TotalAchievementsAwarded = earnCount,
        UsersWithAchievements = uniqueUsers,
        MostEarnedAchievements = new List<AchievementPopularityDto> {
          new() {
            AchievementId = achievementId,
            Name = "", // Will be populated elsewhere if needed
            TimesEarned = earnCount,
            EarnRate = earnRate
          }
        },
        RarestAchievements = new List<AchievementPopularityDto>()
      };
    }

    return results;
  }
}

/// <summary>
/// DataLoader for Achievement Earn Count
/// </summary>
public interface IAchievementEarnCountDataLoader : IDataLoader<Guid, int> { }

public class AchievementEarnCountDataLoader : BatchDataLoader<Guid, int>, IAchievementEarnCountDataLoader {
  private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

  public AchievementEarnCountDataLoader(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IBatchScheduler batchScheduler,
    DataLoaderOptions? options = null)
    : base(batchScheduler, options ?? new DataLoaderOptions()) {
    _dbContextFactory = dbContextFactory;
  }

  protected override async Task<IReadOnlyDictionary<Guid, int>> LoadBatchAsync(
    IReadOnlyList<Guid> keys,
    CancellationToken cancellationToken) {
    await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

    var earnCounts = await context.UserAchievements
      .Where(ua => keys.Contains(ua.AchievementId))
      .GroupBy(ua => ua.AchievementId)
      .Select(g => new { AchievementId = g.Key, Count = g.Count() })
      .ToListAsync(cancellationToken);

    var result = keys.ToDictionary(k => k, k => 0);
    foreach (var item in earnCounts) {
      result[item.AchievementId] = item.Count;
    }

    return result;
  }
}
