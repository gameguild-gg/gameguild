using GameGuild.Modules.Posts.GraphQL;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// GraphQL resolvers for Achievement type
/// </summary>
public class AchievementResolvers {
  public async Task<IEnumerable<AchievementLevel>> GetLevelsAsync(
    [Parent] Achievement achievement,
    [Service] IAchievementLevelDataLoader achievementLevelLoader,
    CancellationToken cancellationToken) {
    return await achievementLevelLoader.LoadAsync(achievement.Id, cancellationToken) ?? Enumerable.Empty<AchievementLevel>();
  }

  public async Task<IEnumerable<AchievementPrerequisite>> GetPrerequisitesAsync(
    [Parent] Achievement achievement,
    [Service] IAchievementPrerequisiteDataLoader prerequisiteLoader,
    CancellationToken cancellationToken) {
    return await prerequisiteLoader.LoadAsync(achievement.Id, cancellationToken) ?? Enumerable.Empty<AchievementPrerequisite>();
  }

  public async Task<IEnumerable<UserAchievement>> GetUserAchievementsAsync(
    [Parent] Achievement achievement,
    [Service] IUserAchievementDataLoader userAchievementLoader,
    int first = 10,
    CancellationToken cancellationToken = default) {
    var userAchievements = await userAchievementLoader.LoadAsync(achievement.Id, cancellationToken);
    return userAchievements?.Take(first) ?? Enumerable.Empty<UserAchievement>();
  }

  public async Task<AchievementStatisticsDto> GetStatisticsAsync(
    [Parent] Achievement achievement,
    [Service] IAchievementStatisticsDataLoader statisticsLoader,
    CancellationToken cancellationToken) {
    return await statisticsLoader.LoadAsync(achievement.Id, cancellationToken) ?? 
           new AchievementStatisticsDto { AchievementId = achievement.Id };
  }

  public async Task<int> GetEarnCountAsync(
    [Parent] Achievement achievement,
    [Service] IAchievementEarnCountDataLoader earnCountLoader,
    CancellationToken cancellationToken) {
    return await earnCountLoader.LoadAsync(achievement.Id, cancellationToken);
  }
}

/// <summary>
/// GraphQL resolvers for UserAchievement type
/// </summary>
public class UserAchievementResolvers {
  public async Task<Achievement?> GetAchievementAsync(
    [Parent] UserAchievement userAchievement,
    [Service] IAchievementDataLoader achievementLoader,
    CancellationToken cancellationToken) {
    return await achievementLoader.LoadAsync(userAchievement.AchievementId, cancellationToken);
  }

  public async Task<User?> GetUserAsync(
    [Parent] UserAchievement userAchievement,
    [Service] IUserDataLoader userLoader,
    CancellationToken cancellationToken) {
    if (userAchievement.UserId == null) return null;
    return await userLoader.LoadAsync(userAchievement.UserId.Value, cancellationToken);
  }

  public double GetProgressPercentageAsync([Parent] UserAchievement userAchievement) {
    return userAchievement.MaxProgress > 0 ? (double)userAchievement.Progress / userAchievement.MaxProgress * 100 : 0;
  }
}

/// <summary>
/// GraphQL resolvers for AchievementLevel type
/// </summary>
public class AchievementLevelResolvers {
  public async Task<Achievement?> GetAchievementAsync(
    [Parent] AchievementLevel achievementLevel,
    [Service] IAchievementDataLoader achievementLoader,
    CancellationToken cancellationToken) {
    return await achievementLoader.LoadAsync(achievementLevel.AchievementId, cancellationToken);
  }
}

/// <summary>
/// GraphQL resolvers for AchievementProgress type
/// </summary>
public class AchievementProgressResolvers {
  public async Task<Achievement?> GetAchievementAsync(
    [Parent] AchievementProgress achievementProgress,
    [Service] IAchievementDataLoader achievementLoader,
    CancellationToken cancellationToken) {
    return await achievementLoader.LoadAsync(achievementProgress.AchievementId, cancellationToken);
  }

  public async Task<User?> GetUserAsync(
    [Parent] AchievementProgress achievementProgress,
    [Service] IUserDataLoader userLoader,
    CancellationToken cancellationToken) {
    if (achievementProgress.UserId == null) return null;
    return await userLoader.LoadAsync(achievementProgress.UserId.Value, cancellationToken);
  }

  public double GetProgressPercentageAsync([Parent] AchievementProgress achievementProgress) {
    return achievementProgress.TargetProgress > 0 ? (double)achievementProgress.CurrentProgress / achievementProgress.TargetProgress * 100 : 0;
  }
}

/// <summary>
/// GraphQL type for Achievement Statistics
/// </summary>
public class AchievementStatisticsType : ObjectType<AchievementStatisticsDto> {
  protected override void Configure(IObjectTypeDescriptor<AchievementStatisticsDto> descriptor) {
    descriptor.Name("AchievementStatistics");
    descriptor.Description("Statistics about achievements");

    descriptor
      .Field(s => s.TotalAchievements)
      .Description("Total number of achievements");

    descriptor
      .Field(s => s.ActiveAchievements)
      .Description("Number of active achievements");

    descriptor
      .Field(s => s.SecretAchievements)
      .Description("Number of secret achievements");

    descriptor
      .Field(s => s.RepeatableAchievements)
      .Description("Number of repeatable achievements");

    descriptor
      .Field(s => s.UsersWithAchievements)
      .Description("Number of users who have earned achievements");

    descriptor
      .Field(s => s.TotalAchievementsAwarded)
      .Description("Total number of achievements awarded");

    descriptor
      .Field(s => s.AchievementsByCategory)
      .Description("Achievements grouped by category");

    descriptor
      .Field(s => s.AchievementsByType)
      .Description("Achievements grouped by type");

    descriptor
      .Field(s => s.MostEarnedAchievements)
      .Description("Most frequently earned achievements");

    descriptor
      .Field(s => s.RarestAchievements)
      .Description("Rarest achievements");
  }
}
