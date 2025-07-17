using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Handler for getting achievements with pagination
/// </summary>
public class GetAchievementsQueryHandler : IQueryHandler<GetAchievementsQuery, GameGuild.Common.Result<AchievementsPageDto>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<GetAchievementsQueryHandler> _logger;

  public GetAchievementsQueryHandler(ApplicationDbContext context, ILogger<GetAchievementsQueryHandler> logger) {
    _context = context;
    _logger = logger;
  }

  public async Task<GameGuild.Common.Result<AchievementsPageDto>> Handle(GetAchievementsQuery request, CancellationToken cancellationToken) {
    try {
      var query = _context.Achievements
        .Include(a => a.Levels)
        .Include(a => a.Prerequisites)
        .ThenInclude(p => p.PrerequisiteAchievement)
        .Where(a => a.TenantId == request.TenantId);

      // Apply filters
      if (!string.IsNullOrEmpty(request.Category)) {
        query = query.Where(a => a.Category == request.Category);
      }

      if (!string.IsNullOrEmpty(request.Type)) {
        query = query.Where(a => a.Type == request.Type);
      }

      if (request.IsActive.HasValue) {
        query = query.Where(a => a.IsActive == request.IsActive.Value);
      }

      if (!request.IncludeSecrets) {
        query = query.Where(a => !a.IsSecret);
      }
      else if (request.IsSecret.HasValue) {
        query = query.Where(a => a.IsSecret == request.IsSecret.Value);
      }

      if (!string.IsNullOrEmpty(request.SearchTerm)) {
        query = query.Where(a => a.Name.Contains(request.SearchTerm) || 
                                (a.Description != null && a.Description.Contains(request.SearchTerm)));
      }

      // Apply ordering
      query = request.OrderBy.ToLower() switch {
        "name" => request.Descending ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name),
        "points" => request.Descending ? query.OrderByDescending(a => a.Points) : query.OrderBy(a => a.Points),
        "createdat" => request.Descending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
        _ => request.Descending ? query.OrderByDescending(a => a.DisplayOrder) : query.OrderBy(a => a.DisplayOrder)
      };

      var totalCount = await query.CountAsync(cancellationToken);

      var achievements = await query
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .ToListAsync(cancellationToken);

      var achievementDtos = achievements.Select(a => new AchievementDto {
        Id = a.Id,
        Name = a.Name,
        Description = a.Description,
        Category = a.Category,
        Type = a.Type,
        IconUrl = a.IconUrl,
        Color = a.Color,
        Points = a.Points,
        IsActive = a.IsActive,
        IsSecret = a.IsSecret,
        IsRepeatable = a.IsRepeatable,
        Conditions = a.Conditions,
        DisplayOrder = a.DisplayOrder,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt,
        Levels = a.Levels?.Select(l => new AchievementLevelDto {
          Id = l.Id,
          Level = l.Level,
          Name = l.Name,
          Description = l.Description,
          RequiredProgress = l.RequiredProgress,
          Points = l.Points,
          IconUrl = l.IconUrl,
          Color = l.Color
        }).ToList(),
        Prerequisites = a.Prerequisites?.Select(p => new AchievementDto {
          Id = p.PrerequisiteAchievement!.Id,
          Name = p.PrerequisiteAchievement.Name,
          Description = p.PrerequisiteAchievement.Description,
          Category = p.PrerequisiteAchievement.Category,
          Type = p.PrerequisiteAchievement.Type,
          IconUrl = p.PrerequisiteAchievement.IconUrl,
          Color = p.PrerequisiteAchievement.Color,
          Points = p.PrerequisiteAchievement.Points,
          IsActive = p.PrerequisiteAchievement.IsActive,
          IsSecret = p.PrerequisiteAchievement.IsSecret,
          IsRepeatable = p.PrerequisiteAchievement.IsRepeatable,
          Conditions = p.PrerequisiteAchievement.Conditions,
          DisplayOrder = p.PrerequisiteAchievement.DisplayOrder,
          CreatedAt = p.PrerequisiteAchievement.CreatedAt,
          UpdatedAt = p.PrerequisiteAchievement.UpdatedAt
        }).ToList()
      }).ToList();

      var result = new AchievementsPageDto(achievementDtos, totalCount, request.PageNumber, request.PageSize);

      return GameGuild.Common.Result.Success(result);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting achievements");
      return GameGuild.Common.Result.Failure<AchievementsPageDto>(GameGuild.Common.Error.Failure("GetAchievements", "Failed to get achievements"));
    }
  }
}

/// <summary>
/// Handler for getting achievement by ID
/// </summary>
public class GetAchievementByIdQueryHandler : IQueryHandler<GetAchievementByIdQuery, GameGuild.Common.Result<Achievement>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<GetAchievementByIdQueryHandler> _logger;

  public GetAchievementByIdQueryHandler(ApplicationDbContext context, ILogger<GetAchievementByIdQueryHandler> logger) {
    _context = context;
    _logger = logger;
  }

  public async Task<GameGuild.Common.Result<Achievement>> Handle(GetAchievementByIdQuery request, CancellationToken cancellationToken) {
    try {
      var query = _context.Achievements
        .Where(a => a.Id == request.AchievementId && a.TenantId == request.TenantId);

      if (request.IncludeLevels) {
        query = query.Include(a => a.Levels);
      }

      if (request.IncludePrerequisites) {
        query = query.Include(a => a.Prerequisites)
          .ThenInclude(p => p.PrerequisiteAchievement);
      }

      var achievement = await query.FirstOrDefaultAsync(cancellationToken);

      if (achievement == null) {
        return GameGuild.Common.Result.Failure<Achievement>(GameGuild.Common.Error.NotFound("AchievementNotFound", "Achievement not found"));
      }

      return GameGuild.Common.Result.Success(achievement);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting achievement {AchievementId}", request.AchievementId);
      return GameGuild.Common.Result.Failure<Achievement>(GameGuild.Common.Error.Failure("GetAchievement", "Failed to get achievement"));
    }
  }
}

/// <summary>
/// Handler for getting user achievements
/// </summary>
public class GetUserAchievementsQueryHandler : IQueryHandler<GetUserAchievementsQuery, GameGuild.Common.Result<UserAchievementsPageDto>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<GetUserAchievementsQueryHandler> _logger;

  public GetUserAchievementsQueryHandler(ApplicationDbContext context, ILogger<GetUserAchievementsQueryHandler> logger) {
    _context = context;
    _logger = logger;
  }

  public async Task<GameGuild.Common.Result<UserAchievementsPageDto>> Handle(GetUserAchievementsQuery request, CancellationToken cancellationToken) {
    try {
      var query = _context.UserAchievements
        .Include(ua => ua.Achievement)
        .ThenInclude(a => a!.Levels)
        .Where(ua => ua.UserId == request.UserId && ua.TenantId == request.TenantId);

      // Apply filters
      if (!string.IsNullOrEmpty(request.Category)) {
        query = query.Where(ua => ua.Achievement!.Category == request.Category);
      }

      if (!string.IsNullOrEmpty(request.Type)) {
        query = query.Where(ua => ua.Achievement!.Type == request.Type);
      }

      if (request.IsCompleted.HasValue) {
        query = query.Where(ua => ua.IsCompleted == request.IsCompleted.Value);
      }

      if (request.EarnedAfter.HasValue) {
        query = query.Where(ua => ua.EarnedAt >= request.EarnedAfter.Value);
      }

      if (request.EarnedBefore.HasValue) {
        query = query.Where(ua => ua.EarnedAt <= request.EarnedBefore.Value);
      }

      // Apply ordering
      query = request.OrderBy.ToLower() switch {
        "points" => request.Descending ? query.OrderByDescending(ua => ua.PointsEarned) : query.OrderBy(ua => ua.PointsEarned),
        "achievementname" => request.Descending ? query.OrderByDescending(ua => ua.Achievement!.Name) : query.OrderBy(ua => ua.Achievement!.Name),
        _ => request.Descending ? query.OrderByDescending(ua => ua.EarnedAt) : query.OrderBy(ua => ua.EarnedAt)
      };

      var totalCount = await query.CountAsync(cancellationToken);

      var userAchievements = await query
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .ToListAsync(cancellationToken);

      var userAchievementDtos = userAchievements.Select(ua => new UserAchievementDto {
        Id = ua.Id,
        UserId = ua.UserId,
        AchievementId = ua.AchievementId,
        Achievement = ua.Achievement != null ? new AchievementDto {
          Id = ua.Achievement.Id,
          Name = ua.Achievement.Name,
          Description = ua.Achievement.Description,
          Category = ua.Achievement.Category,
          Type = ua.Achievement.Type,
          IconUrl = ua.Achievement.IconUrl,
          Color = ua.Achievement.Color,
          Points = ua.Achievement.Points,
          IsActive = ua.Achievement.IsActive,
          IsSecret = ua.Achievement.IsSecret,
          IsRepeatable = ua.Achievement.IsRepeatable,
          Conditions = ua.Achievement.Conditions,
          DisplayOrder = ua.Achievement.DisplayOrder,
          CreatedAt = ua.Achievement.CreatedAt,
          UpdatedAt = ua.Achievement.UpdatedAt,
          Levels = ua.Achievement.Levels?.Select(l => new AchievementLevelDto {
            Id = l.Id,
            Level = l.Level,
            Name = l.Name,
            Description = l.Description,
            RequiredProgress = l.RequiredProgress,
            Points = l.Points,
            IconUrl = l.IconUrl,
            Color = l.Color
          }).ToList()
        } : null,
        EarnedAt = ua.EarnedAt,
        Level = ua.Level,
        Progress = ua.Progress,
        MaxProgress = ua.MaxProgress,
        IsCompleted = ua.IsCompleted,
        IsNotified = ua.IsNotified,
        Context = ua.Context,
        PointsEarned = ua.PointsEarned,
        EarnCount = ua.EarnCount
      }).ToList();

      var result = new UserAchievementsPageDto(userAchievementDtos, totalCount, request.PageNumber, request.PageSize);

      return GameGuild.Common.Result.Success(result);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting user achievements for user {UserId}", request.UserId);
      return GameGuild.Common.Result.Failure<UserAchievementsPageDto>(GameGuild.Common.Error.Failure("GetUserAchievements", "Failed to get user achievements"));
    }
  }
}

/// <summary>
/// Handler for getting user achievement progress
/// </summary>
public class GetUserAchievementProgressQueryHandler : IQueryHandler<GetUserAchievementProgressQuery, GameGuild.Common.Result<List<AchievementProgressDto>>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<GetUserAchievementProgressQueryHandler> _logger;

  public GetUserAchievementProgressQueryHandler(ApplicationDbContext context, ILogger<GetUserAchievementProgressQueryHandler> logger) {
    _context = context;
    _logger = logger;
  }

  public async Task<GameGuild.Common.Result<List<AchievementProgressDto>>> Handle(GetUserAchievementProgressQuery request, CancellationToken cancellationToken) {
    try {
      var query = _context.AchievementProgress
        .Include(ap => ap.Achievement)
        .Where(ap => ap.UserId == request.UserId && ap.TenantId == request.TenantId);

      // Apply filters
      if (!string.IsNullOrEmpty(request.Category)) {
        query = query.Where(ap => ap.Achievement!.Category == request.Category);
      }

      if (request.OnlyInProgress) {
        query = query.Where(ap => ap.CurrentProgress > 0 && !ap.IsCompleted);
      }

      var progressRecords = await query.ToListAsync(cancellationToken);

      var progressDtos = progressRecords.Select(ap => new AchievementProgressDto {
        Id = ap.Id,
        UserId = ap.UserId,
        AchievementId = ap.AchievementId,
        Achievement = ap.Achievement != null ? new AchievementDto {
          Id = ap.Achievement.Id,
          Name = ap.Achievement.Name,
          Description = ap.Achievement.Description,
          Category = ap.Achievement.Category,
          Type = ap.Achievement.Type,
          IconUrl = ap.Achievement.IconUrl,
          Color = ap.Achievement.Color,
          Points = ap.Achievement.Points,
          IsActive = ap.Achievement.IsActive,
          IsSecret = ap.Achievement.IsSecret,
          IsRepeatable = ap.Achievement.IsRepeatable,
          Conditions = ap.Achievement.Conditions,
          DisplayOrder = ap.Achievement.DisplayOrder,
          CreatedAt = ap.Achievement.CreatedAt,
          UpdatedAt = ap.Achievement.UpdatedAt
        } : null,
        CurrentProgress = ap.CurrentProgress,
        TargetProgress = ap.TargetProgress,
        LastUpdated = ap.LastUpdated,
        IsCompleted = ap.IsCompleted,
        Context = ap.Context
      }).ToList();

      return GameGuild.Common.Result.Success(progressDtos);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting achievement progress for user {UserId}", request.UserId);
      return GameGuild.Common.Result.Failure<List<AchievementProgressDto>>(GameGuild.Common.Error.Failure("GetAchievementProgress", "Failed to get achievement progress"));
    }
  }
}

/// <summary>
/// Handler for getting user achievement summary
/// </summary>
public class GetUserAchievementSummaryQueryHandler : IQueryHandler<GetUserAchievementSummaryQuery, GameGuild.Common.Result<UserAchievementSummaryDto>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<GetUserAchievementSummaryQueryHandler> _logger;

  public GetUserAchievementSummaryQueryHandler(ApplicationDbContext context, ILogger<GetUserAchievementSummaryQueryHandler> logger) {
    _context = context;
    _logger = logger;
  }

  public async Task<GameGuild.Common.Result<UserAchievementSummaryDto>> Handle(GetUserAchievementSummaryQuery request, CancellationToken cancellationToken) {
    try {
      var userAchievements = await _context.UserAchievements
        .Include(ua => ua.Achievement)
        .Where(ua => ua.UserId == request.UserId && ua.TenantId == request.TenantId)
        .ToListAsync(cancellationToken);

      var progressRecords = await _context.AchievementProgress
        .Include(ap => ap.Achievement)
        .Where(ap => ap.UserId == request.UserId && ap.TenantId == request.TenantId)
        .ToListAsync(cancellationToken);

      var totalAchievements = userAchievements.Count;
      var totalPoints = userAchievements.Sum(ua => ua.PointsEarned);
      var completedAchievements = userAchievements.Count(ua => ua.IsCompleted);
      var inProgressAchievements = progressRecords.Count(ap => ap.CurrentProgress > 0 && !ap.IsCompleted);

      // Recent achievements
      var recentAchievements = userAchievements
        .Where(ua => ua.IsCompleted)
        .OrderByDescending(ua => ua.EarnedAt)
        .Take(request.RecentLimit)
        .Select(ua => new UserAchievementDto {
          Id = ua.Id,
          UserId = ua.UserId,
          AchievementId = ua.AchievementId,
          Achievement = ua.Achievement != null ? new AchievementDto {
            Id = ua.Achievement.Id,
            Name = ua.Achievement.Name,
            Description = ua.Achievement.Description,
            Category = ua.Achievement.Category,
            Type = ua.Achievement.Type,
            IconUrl = ua.Achievement.IconUrl,
            Color = ua.Achievement.Color,
            Points = ua.Achievement.Points,
            IsActive = ua.Achievement.IsActive,
            IsSecret = ua.Achievement.IsSecret,
            IsRepeatable = ua.Achievement.IsRepeatable,
            Conditions = ua.Achievement.Conditions,
            DisplayOrder = ua.Achievement.DisplayOrder,
            CreatedAt = ua.Achievement.CreatedAt,
            UpdatedAt = ua.Achievement.UpdatedAt
          } : null,
          EarnedAt = ua.EarnedAt,
          Level = ua.Level,
          Progress = ua.Progress,
          MaxProgress = ua.MaxProgress,
          IsCompleted = ua.IsCompleted,
          IsNotified = ua.IsNotified,
          Context = ua.Context,
          PointsEarned = ua.PointsEarned,
          EarnCount = ua.EarnCount
        }).ToList();

      // Near completion achievements
      var nearCompletion = progressRecords
        .Where(ap => ap.TargetProgress > 0)
        .Where(ap => (double)ap.CurrentProgress / ap.TargetProgress * 100 >= request.NearCompletionThreshold && !ap.IsCompleted)
        .Select(ap => new AchievementProgressDto {
          Id = ap.Id,
          UserId = ap.UserId,
          AchievementId = ap.AchievementId,
          Achievement = ap.Achievement != null ? new AchievementDto {
            Id = ap.Achievement.Id,
            Name = ap.Achievement.Name,
            Description = ap.Achievement.Description,
            Category = ap.Achievement.Category,
            Type = ap.Achievement.Type,
            IconUrl = ap.Achievement.IconUrl,
            Color = ap.Achievement.Color,
            Points = ap.Achievement.Points,
            IsActive = ap.Achievement.IsActive,
            IsSecret = ap.Achievement.IsSecret,
            IsRepeatable = ap.Achievement.IsRepeatable,
            Conditions = ap.Achievement.Conditions,
            DisplayOrder = ap.Achievement.DisplayOrder,
            CreatedAt = ap.Achievement.CreatedAt,
            UpdatedAt = ap.Achievement.UpdatedAt
          } : null,
          CurrentProgress = ap.CurrentProgress,
          TargetProgress = ap.TargetProgress,
          LastUpdated = ap.LastUpdated,
          IsCompleted = ap.IsCompleted,
          Context = ap.Context
        }).ToList();

      // Achievements by category
      var achievementsByCategory = userAchievements
        .Where(ua => ua.Achievement != null)
        .GroupBy(ua => ua.Achievement!.Category)
        .ToDictionary(g => g.Key, g => g.Count());

      var summary = new UserAchievementSummaryDto {
        UserId = request.UserId,
        TotalAchievements = totalAchievements,
        TotalPoints = totalPoints,
        CompletedAchievements = completedAchievements,
        InProgressAchievements = inProgressAchievements,
        RecentAchievements = recentAchievements,
        NearCompletion = nearCompletion,
        AchievementsByCategory = achievementsByCategory
      };

      return GameGuild.Common.Result.Success(summary);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting achievement summary for user {UserId}", request.UserId);
      return GameGuild.Common.Result.Failure<UserAchievementSummaryDto>(GameGuild.Common.Error.Failure("GetAchievementSummary", "Failed to get achievement summary"));
    }
  }
}

/// <summary>
/// Handler for getting available achievements for a user
/// </summary>
public class GetAvailableAchievementsQueryHandler : IQueryHandler<GetAvailableAchievementsQuery, GameGuild.Common.Result<AchievementsPageDto>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<GetAvailableAchievementsQueryHandler> _logger;

  public GetAvailableAchievementsQueryHandler(ApplicationDbContext context, ILogger<GetAvailableAchievementsQueryHandler> logger) {
    _context = context;
    _logger = logger;
  }

  public async Task<GameGuild.Common.Result<AchievementsPageDto>> Handle(GetAvailableAchievementsQuery request, CancellationToken cancellationToken) {
    try {
      // Get user's earned achievements
      var earnedAchievementIds = await _context.UserAchievements
        .Where(ua => ua.UserId == request.UserId && ua.TenantId == request.TenantId && ua.IsCompleted)
        .Select(ua => ua.AchievementId)
        .ToListAsync(cancellationToken);

      var query = _context.Achievements
        .Where(a => a.TenantId == request.TenantId && a.IsActive)
        .Where(a => !earnedAchievementIds.Contains(a.Id)); // Exclude earned achievements

      // Apply filters
      if (!string.IsNullOrEmpty(request.Category)) {
        query = query.Where(a => a.Category == request.Category);
      }

      if (request.ExcludeSecret) {
        query = query.Where(a => !a.IsSecret);
      }

      // Get total count before pagination
      var totalCount = await query.CountAsync(cancellationToken);

      // Apply pagination
      var achievements = await query
        .OrderBy(a => a.DisplayOrder)
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .ToListAsync(cancellationToken);

      var achievementDtos = achievements.Select(a => new AchievementDto {
        Id = a.Id,
        Name = a.Name,
        Description = a.Description,
        Category = a.Category,
        Type = a.Type,
        IconUrl = a.IconUrl,
        Color = a.Color,
        Points = a.Points,
        IsActive = a.IsActive,
        IsSecret = a.IsSecret,
        IsRepeatable = a.IsRepeatable,
        Conditions = a.Conditions,
        DisplayOrder = a.DisplayOrder,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
      }).ToList();

      var result = new AchievementsPageDto(achievementDtos, totalCount, request.PageNumber, request.PageSize);
      return GameGuild.Common.Result.Success(result);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting available achievements for user {UserId}", request.UserId);
      return GameGuild.Common.Result.Failure<AchievementsPageDto>(GameGuild.Common.Error.Failure("GetAvailableAchievements", "Failed to get available achievements"));
    }
  }
}

/// <summary>
/// Handler for getting achievement leaderboard
/// </summary>
public class GetAchievementLeaderboardQueryHandler : IQueryHandler<GetAchievementLeaderboardQuery, GameGuild.Common.Result<List<UserAchievementLeaderboardDto>>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<GetAchievementLeaderboardQueryHandler> _logger;

  public GetAchievementLeaderboardQueryHandler(ApplicationDbContext context, ILogger<GetAchievementLeaderboardQueryHandler> logger) {
    _context = context;
    _logger = logger;
  }

  public async Task<GameGuild.Common.Result<List<UserAchievementLeaderboardDto>>> Handle(GetAchievementLeaderboardQuery request, CancellationToken cancellationToken) {
    try {
      var query = _context.UserAchievements
        .Include(ua => ua.User)
        .Where(ua => ua.TenantId == request.TenantId);

      // Apply filters
      if (!string.IsNullOrEmpty(request.Category)) {
        query = query.Include(ua => ua.Achievement)
          .Where(ua => ua.Achievement!.Category == request.Category);
      }

      var userStats = await query
        .GroupBy(ua => ua.UserId)
        .Select(g => new {
          UserId = g.Key,
          TotalAchievements = g.Count(),
          TotalPoints = g.Sum(ua => ua.PointsEarned),
          User = g.First().User
        })
        .OrderByDescending(s => s.TotalPoints)
        .ThenByDescending(s => s.TotalAchievements)
        .Take(request.Limit)
        .ToListAsync(cancellationToken);

      var leaderboard = userStats.Select((stats, index) => new UserAchievementLeaderboardDto {
        Rank = index + 1,
        UserId = stats.UserId,
        UserDisplayName = stats.User?.Name ?? "Unknown User",
        TotalAchievements = stats.TotalAchievements,
        TotalPoints = stats.TotalPoints
      }).ToList();

      return GameGuild.Common.Result.Success(leaderboard);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting achievement leaderboard");
      return GameGuild.Common.Result.Failure<List<UserAchievementLeaderboardDto>>(GameGuild.Common.Error.Failure("GetAchievementLeaderboard", "Failed to get achievement leaderboard"));
    }
  }
}

/// <summary>
/// Handler for getting achievement statistics
/// </summary>
public class GetAchievementStatisticsQueryHandler : IQueryHandler<GetAchievementStatisticsQuery, GameGuild.Common.Result<AchievementStatisticsDto>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<GetAchievementStatisticsQueryHandler> _logger;

  public GetAchievementStatisticsQueryHandler(ApplicationDbContext context, ILogger<GetAchievementStatisticsQueryHandler> logger) {
    _context = context;
    _logger = logger;
  }

  public async Task<GameGuild.Common.Result<AchievementStatisticsDto>> Handle(GetAchievementStatisticsQuery request, CancellationToken cancellationToken) {
    try {
      var achievement = await _context.Achievements
        .FirstOrDefaultAsync(a => a.Id == request.AchievementId && a.TenantId == request.TenantId, cancellationToken);

      if (achievement == null) {
        return GameGuild.Common.Result.Failure<AchievementStatisticsDto>(GameGuild.Common.Error.NotFound("AchievementNotFound", "Achievement not found"));
      }

      var totalEarned = await _context.UserAchievements
        .CountAsync(ua => ua.AchievementId == request.AchievementId && ua.TenantId == request.TenantId && ua.IsCompleted, cancellationToken);

      var totalUsers = await _context.Users
        .CountAsync(cancellationToken);

      var inProgress = await _context.AchievementProgress
        .CountAsync(ap => ap.AchievementId == request.AchievementId && ap.TenantId == request.TenantId && !ap.IsCompleted && ap.CurrentProgress > 0, cancellationToken);

      var completionRate = totalUsers > 0 ? (double)totalEarned / totalUsers * 100 : 0;

      var firstEarned = await _context.UserAchievements
        .Where(ua => ua.AchievementId == request.AchievementId && ua.TenantId == request.TenantId && ua.IsCompleted)
        .OrderBy(ua => ua.EarnedAt)
        .Select(ua => ua.EarnedAt)
        .FirstOrDefaultAsync(cancellationToken);

      var lastEarned = await _context.UserAchievements
        .Where(ua => ua.AchievementId == request.AchievementId && ua.TenantId == request.TenantId && ua.IsCompleted)
        .OrderByDescending(ua => ua.EarnedAt)
        .Select(ua => ua.EarnedAt)
        .FirstOrDefaultAsync(cancellationToken);

      var statistics = new AchievementStatisticsDto {
        AchievementId = request.AchievementId,
        TotalEarned = totalEarned,
        TotalUsers = totalUsers,
        InProgress = inProgress,
        CompletionRate = completionRate,
        FirstEarned = firstEarned,
        LastEarned = lastEarned
      };

      return GameGuild.Common.Result.Success(statistics);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting achievement statistics for achievement {AchievementId}", request.AchievementId);
      return GameGuild.Common.Result.Failure<AchievementStatisticsDto>(GameGuild.Common.Error.Failure("GetAchievementStatistics", "Failed to get achievement statistics"));
    }
  }
}

/// <summary>
/// Handler for checking achievement prerequisites
/// </summary>
public class CheckAchievementPrerequisitesQueryHandler : IQueryHandler<CheckAchievementPrerequisitesQuery, GameGuild.Common.Result<AchievementPrerequisiteCheckDto>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<CheckAchievementPrerequisitesQueryHandler> _logger;

  public CheckAchievementPrerequisitesQueryHandler(ApplicationDbContext context, ILogger<CheckAchievementPrerequisitesQueryHandler> logger) {
    _context = context;
    _logger = logger;
  }

  public async Task<GameGuild.Common.Result<AchievementPrerequisiteCheckDto>> Handle(CheckAchievementPrerequisitesQuery request, CancellationToken cancellationToken) {
    try {
      var achievement = await _context.Achievements
        .Where(a => a.Id == request.AchievementId)
        .FirstOrDefaultAsync(cancellationToken);

      if (achievement == null) {
        return GameGuild.Common.Result.Failure<AchievementPrerequisiteCheckDto>(GameGuild.Common.Error.NotFound("Achievement", "Achievement not found"));
      }

      var prerequisites = await _context.AchievementPrerequisites
        .Include(ap => ap.PrerequisiteAchievement)
        .Where(ap => ap.AchievementId == request.AchievementId)
        .ToListAsync(cancellationToken);

      var userAchievements = await _context.UserAchievements
        .Where(ua => ua.UserId == request.UserId && ua.TenantId == request.TenantId && ua.IsCompleted)
        .Select(ua => ua.AchievementId)
        .ToListAsync(cancellationToken);

      var prerequisiteStatuses = new List<PrerequisiteStatusDto>();
      bool canEarn = true;

      foreach (var prerequisite in prerequisites) {
        bool isMet = userAchievements.Contains(prerequisite.PrerequisiteAchievementId);
        if (!isMet) {
          canEarn = false;
        }

        prerequisiteStatuses.Add(new PrerequisiteStatusDto {
          PrerequisiteAchievementId = prerequisite.PrerequisiteAchievementId,
          Name = prerequisite.PrerequisiteAchievement?.Name ?? "Unknown",
          IsMet = isMet
        });
      }

      var result = new AchievementPrerequisiteCheckDto {
        AchievementId = request.AchievementId,
        CanEarn = canEarn,
        Prerequisites = prerequisiteStatuses
      };

      return GameGuild.Common.Result.Success(result);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error checking prerequisites for achievement {AchievementId} and user {UserId}", request.AchievementId, request.UserId);
      return GameGuild.Common.Result.Failure<AchievementPrerequisiteCheckDto>(GameGuild.Common.Error.Failure("CheckAchievementPrerequisites", "Failed to check achievement prerequisites"));
    }
  }
}
