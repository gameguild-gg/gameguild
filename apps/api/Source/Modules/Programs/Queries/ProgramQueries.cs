using GameGuild.Common;
using GameGuild.Modules.Contents;
using ProgramAvailabilityStatus = GameGuild.Common.EnrollmentStatus;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Queries for Program data retrieval using CQRS pattern
/// All queries implement IRequest pattern for MediatR handling
/// </summary>

// ===== BASIC QUERIES =====

/// <summary>
/// Query to get all programs with pagination and filtering
/// </summary>
public record GetAllProgramsQuery(
  int Skip = 0,
  int Take = 50,
  string? Search = null,
  ProgramCategory? Category = null,
  ProgramDifficulty? Difficulty = null,
  ContentStatus? Status = null,
  AccessLevel? Visibility = null,
  ProgramAvailabilityStatus? EnrollmentStatus = null,
  string? CreatorId = null,
  bool IncludeArchived = false,
  string? SortBy = "CreatedAt",
  bool SortDescending = true
) : IRequest<IEnumerable<Program>>;

/// <summary>
/// Query to get a program by ID
/// </summary>
public record GetProgramByIdQuery(
  Guid Id,
  bool IncludeContent = false,
  bool IncludeEnrollments = false,
  bool IncludeRatings = false
) : IRequest<Program?>;

/// <summary>
/// Query to get a program by slug
/// </summary>
public record GetProgramBySlugQuery(
  string Slug,
  bool IncludeContent = false,
  bool IncludeEnrollments = false,
  bool IncludeRatings = false
) : IRequest<Program?>;

/// <summary>
/// Query to get published program by slug (public access)
/// </summary>
public record GetPublishedProgramBySlugQuery(
  string Slug,
  bool IncludeContent = false
) : IRequest<Program?>;

// ===== SEARCH AND FILTER QUERIES =====

/// <summary>
/// Query to search programs with advanced filtering
/// </summary>
public record SearchProgramsQuery(
  string SearchTerm,
  ProgramCategory? Category = null,
  ProgramDifficulty? Difficulty = null,
  float? MinEstimatedHours = null,
  float? MaxEstimatedHours = null,
  decimal? MinRating = null,
  bool AvailableForEnrollment = false,
  int Skip = 0,
  int Take = 50
) : IRequest<IEnumerable<Program>>;

/// <summary>
/// Query to get programs by category
/// </summary>
public record GetProgramsByCategoryQuery(
  ProgramCategory Category,
  int Skip = 0,
  int Take = 50,
  bool OnlyPublished = true
) : IRequest<IEnumerable<Program>>;

/// <summary>
/// Query to get programs by difficulty
/// </summary>
public record GetProgramsByDifficultyQuery(
  ProgramDifficulty Difficulty,
  int Skip = 0,
  int Take = 50,
  bool OnlyPublished = true
) : IRequest<IEnumerable<Program>>;

/// <summary>
/// Query to get programs by creator
/// </summary>
public record GetProgramsByCreatorQuery(
  string CreatorId,
  int Skip = 0,
  int Take = 50,
  bool OnlyPublished = false
) : IRequest<IEnumerable<Program>>;

// ===== ENROLLMENT QUERIES =====

/// <summary>
/// Query to get enrolled programs for a user
/// </summary>
public record GetUserEnrolledProgramsQuery(
  string UserId,
  int Skip = 0,
  int Take = 50,
  bool OnlyActive = true
) : IRequest<IEnumerable<Program>>;

/// <summary>
/// Query to get program enrollments
/// </summary>
public record GetProgramEnrollmentsQuery(
  Guid ProgramId,
  int Skip = 0,
  int Take = 50,
  bool OnlyActive = true
) : IRequest<IEnumerable<ProgramUser>>;

/// <summary>
/// Query to check if user is enrolled in program
/// </summary>
public record CheckUserEnrollmentQuery(
  Guid ProgramId,
  string UserId
) : IRequest<ProgramUser?>;

// ===== CONTENT QUERIES =====

/// <summary>
/// Query to get program content
/// </summary>
public record GetProgramContentQuery(
  Guid ProgramId,
  bool OnlyVisible = true,
  string? UserId = null
) : IRequest<IEnumerable<ProgramContent>>;

/// <summary>
/// Query to get user's progress in a program
/// </summary>
public record GetUserProgramProgressQuery(
  Guid ProgramId,
  string UserId
) : IRequest<ProgramUserProgress?>;

// ===== STATISTICS QUERIES =====

/// <summary>
/// Query to get program statistics
/// </summary>
public record GetProgramStatisticsQuery(
  Guid ProgramId
) : IRequest<ProgramStatistics>;

/// <summary>
/// Query to get global program statistics
/// </summary>
public record GetGlobalProgramStatisticsQuery(
  DateTime? FromDate = null,
  DateTime? ToDate = null
) : IRequest<GlobalProgramStatistics>;

/// <summary>
/// Query to get creator's program statistics
/// </summary>
public record GetCreatorProgramStatisticsQuery(
  string CreatorId,
  DateTime? FromDate = null,
  DateTime? ToDate = null
) : IRequest<CreatorProgramStatistics>;

// ===== TRENDING AND RECOMMENDATIONS =====

/// <summary>
/// Query to get popular programs
/// </summary>
public record GetPopularProgramsQuery(
  int Skip = 0,
  int Take = 10,
  int DaysBack = 30
) : IRequest<IEnumerable<Program>>;

/// <summary>
/// Query to get recent programs
/// </summary>
public record GetRecentProgramsQuery(
  int Skip = 0,
  int Take = 10,
  int DaysBack = 7
) : IRequest<IEnumerable<Program>>;

/// <summary>
/// Query to get featured programs
/// </summary>
public record GetFeaturedProgramsQuery(
  int Skip = 0,
  int Take = 10
) : IRequest<IEnumerable<Program>>;

/// <summary>
/// Query to get recommended programs for user
/// </summary>
public record GetRecommendedProgramsQuery(
  string UserId,
  int Take = 10
) : IRequest<IEnumerable<Program>>;

// ===== RATING QUERIES =====

/// <summary>
/// Query to get program ratings
/// </summary>
public record GetProgramRatingsQuery(
  Guid ProgramId,
  int Skip = 0,
  int Take = 50
) : IRequest<IEnumerable<ProgramRating>>;

/// <summary>
/// Query to get user's rating for a program
/// </summary>
public record GetUserProgramRatingQuery(
  Guid ProgramId,
  string UserId
) : IRequest<ProgramRating?>;

// ===== WISHLIST QUERIES =====

/// <summary>
/// Query to get user's wishlist programs
/// </summary>
public record GetUserWishlistQuery(
  string UserId,
  int Skip = 0,
  int Take = 50
) : IRequest<IEnumerable<Program>>;

/// <summary>
/// Query to check if program is in user's wishlist
/// </summary>
public record CheckProgramInWishlistQuery(
  Guid ProgramId,
  string UserId
) : IRequest<bool>;

// ===== DTO CLASSES FOR STATISTICS =====

/// <summary>
/// Statistics for a specific program
/// </summary>
public record ProgramStatistics(
  Guid ProgramId,
  int TotalEnrollments,
  int ActiveEnrollments,
  int CompletedEnrollments,
  decimal AverageRating,
  int TotalRatings,
  decimal CompletionRate,
  TimeSpan AverageCompletionTime
);

/// <summary>
/// Global statistics for all programs
/// </summary>
public record GlobalProgramStatistics(
  int TotalPrograms,
  int PublishedPrograms,
  int TotalEnrollments,
  int ActiveEnrollments,
  decimal AverageRating,
  int TotalRatings,
  ProgramCategory MostPopularCategory,
  ProgramDifficulty MostPopularDifficulty
);

/// <summary>
/// Statistics for a creator's programs
/// </summary>
public record CreatorProgramStatistics(
  string CreatorId,
  int TotalPrograms,
  int PublishedPrograms,
  int TotalEnrollments,
  int ActiveEnrollments,
  decimal AverageRating,
  int TotalRatings,
  decimal AverageCompletionRate
);

/// <summary>
/// User's progress in a program
/// </summary>
public record ProgramUserProgress(
  Guid ProgramId,
  string UserId,
  int CompletedContent,
  int TotalContent,
  decimal ProgressPercentage,
  TimeSpan TimeSpent,
  DateTime? LastActivityAt,
  bool IsCompleted,
  DateTime? CompletedAt
);
