using GameGuild.Common.Enums;
using GameGuild.Modules.Contents.Models;
using GameGuild.Modules.Programs.DTOs;
using GameGuild.Modules.Programs.Models;
using ProgramEntity = GameGuild.Modules.Programs.Models.Program;


namespace GameGuild.Modules.Programs.Services;

/// <summary>
/// Service interface for Program business logic
/// Provides operations for managing programs, content, user participation, and analytics
/// </summary>
public interface IProgramService {
  // Basic CRUD Operations
  /// <summary>
  /// Get a program by ID with basic details
  /// </summary>
  Task<ProgramEntity?> GetProgramByIdAsync(Guid id);

  /// <summary>
  /// Get a program by ID with all content included
  /// </summary>
  Task<ProgramEntity?> GetProgramWithContentAsync(Guid id);

  /// <summary>
  /// Get paginated list of programs
  /// </summary>
  Task<IEnumerable<ProgramEntity>> GetProgramsAsync(int skip = 0, int take = 50);

  /// <summary>
  /// Create a new program
  /// </summary>
  Task<ProgramEntity> CreateProgramAsync(ProgramEntity program);

  /// <summary>
  /// Update an existing program
  /// </summary>
  Task<ProgramEntity> UpdateProgramAsync(ProgramEntity program);

  /// <summary>
  /// Soft delete a program
  /// </summary>
  Task DeleteProgramAsync(Guid id);

  /// <summary>
  /// Clone/duplicate a program with a new title
  /// </summary>
  Task<ProgramEntity> CloneProgramAsync(Guid id, string newTitle);

  /// <summary>
  /// Check if a program exists and is not deleted
  /// </summary>
  Task<bool> ProgramExistsAsync(Guid id);

  // Content Management Operations
  /// <summary>
  /// Add content to a program
  /// </summary>
  Task<ProgramContent> AddContentAsync(Guid programId, ProgramContent content);

  /// <summary>
  /// Update program content
  /// </summary>
  Task<ProgramContent> UpdateContentAsync(ProgramContent content);

  /// <summary>
  /// Delete program content
  /// </summary>
  Task DeleteContentAsync(Guid contentId);

  /// <summary>
  /// Reorder content within a program
  /// </summary>
  Task<ProgramEntity> ReorderContentAsync(Guid programId, List<Guid> contentIds);

  /// <summary>
  /// Get all content for a program
  /// </summary>
  Task<IEnumerable<ProgramContent>> GetProgramContentAsync(Guid programId);

  // User Participation Management
  /// <summary>
  /// Add a user to a program
  /// </summary>
  Task<ProgramUser> AddUserAsync(Guid programId, Guid userId);

  /// <summary>
  /// Remove a user from a program
  /// </summary>
  Task<ProgramUser> RemoveUserAsync(Guid programId, Guid userId);

  /// <summary>
  /// Get all users participating in a program
  /// </summary>
  Task<IEnumerable<ProgramUser>> GetProgramUsersAsync(Guid programId);

  /// <summary>
  /// Get all programs a user is participating in
  /// </summary>
  Task<IEnumerable<ProgramEntity>> GetUserProgramsAsync(Guid userId);

  /// <summary>
  /// Check if a user is participating in a program
  /// </summary>
  Task<bool> IsUserInProgramAsync(Guid programId, Guid userId);

  // Progress & Analytics
  /// <summary>
  /// Get user's progress information for a program
  /// </summary>
  Task<UserProgressDto?> GetUserProgressDtoAsync(Guid programId, Guid userId);

  /// <summary>
  /// Get user's content interactions in a program
  /// </summary>
  Task<IEnumerable<ContentInteraction>> GetUserInteractionsAsync(Guid programId, Guid userId);

  /// <summary>
  /// Update user's progress for specific content
  /// </summary>
  Task<ProgramEntity> UpdateUserProgressAsync(Guid programId, Guid userId, Guid contentId, ProgressStatus status);

  // Lifecycle Management
  /// <summary>
  /// Create a draft program
  /// </summary>
  Task<ProgramEntity> CreateDraftAsync(ProgramEntity program);

  /// <summary>
  /// Submit program for review
  /// </summary>
  Task<ProgramEntity> SubmitForReviewAsync(Guid id);

  /// <summary>
  /// Approve a program
  /// </summary>
  Task<ProgramEntity> ApproveAsync(Guid id);

  /// <summary>
  /// Reject a program with reason
  /// </summary>
  Task<ProgramEntity> RejectAsync(Guid id, string reason);

  /// <summary>
  /// Archive a program
  /// </summary>
  Task<ProgramEntity> ArchiveAsync(Guid id);

  /// <summary>
  /// Restore an archived program
  /// </summary>
  Task<ProgramEntity> RestoreAsync(Guid id);

  // Publishing Operations
  /// <summary>
  /// Publish a program
  /// </summary>
  Task<ProgramEntity> PublishProgramAsync(Guid id);

  /// <summary>
  /// Unpublish a program
  /// </summary>
  Task<ProgramEntity> UnpublishProgramAsync(Guid id);

  /// <summary>
  /// Schedule program for publishing
  /// </summary>
  Task<ProgramEntity> SchedulePublishAsync(Guid id, DateTime publishAt);

  /// <summary>
  /// Set program visibility/access level
  /// </summary>
  Task<ProgramEntity> SetVisibilityAsync(Guid id, AccessLevel visibility);

  // Search & Discovery
  /// <summary>
  /// Search programs by term
  /// </summary>
  Task<IEnumerable<ProgramEntity>> SearchProgramsAsync(string searchTerm, int skip = 0, int take = 50);

  /// <summary>
  /// Get programs created by a specific user
  /// </summary>
  Task<IEnumerable<ProgramEntity>> GetProgramsByCreatorAsync(Guid creatorId, int skip = 0, int take = 50);

  /// <summary>
  /// Get featured programs
  /// </summary>
  Task<IEnumerable<ProgramEntity>> GetFeaturedProgramsAsync(int count = 10);

  /// <summary>
  /// Get recently created programs
  /// </summary>
  Task<IEnumerable<ProgramEntity>> GetRecentProgramsAsync(int count = 10);

  /// <summary>
  /// Get popular programs based on user participation
  /// </summary>
  Task<IEnumerable<ProgramEntity>> GetPopularProgramsAsync(int count = 10);

  // Analytics & Statistics
  /// <summary>
  /// Get total program count with optional filters
  /// </summary>
  Task<int> GetProgramCountAsync(ContentStatus? status = null, AccessLevel? visibility = null);

  /// <summary>
  /// Get user count for a specific program
  /// </summary>
  Task<int> GetUserCountForProgramAsync(Guid programId);

  /// <summary>
  /// Get average completion rate for a program
  /// </summary>
  Task<decimal> GetAverageCompletionRateAsync(Guid programId);

  /// <summary>
  /// Get program completion statistics
  /// </summary>
  Task<Dictionary<string, object>> GetProgramStatisticsAsync(Guid programId);

  // Additional methods for controller support

  // CRUD Operations with DTOs
  /// <summary>
  /// Create a program using DTO
  /// </summary>
  Task<ProgramEntity> CreateProgramAsync(CreateProgramDto createDto);

  /// <summary>
  /// Update a program using DTO
  /// </summary>
  Task<ProgramEntity?> UpdateProgramAsync(Guid id, UpdateProgramDto updateDto);

  // Category and Difficulty Operations
  /// <summary>
  /// Get programs by category
  /// </summary>
  Task<IEnumerable<ProgramEntity>> GetProgramsByCategoryAsync(ProgramCategory category, int skip = 0, int take = 50);

  /// <summary>
  /// Get programs by difficulty level
  /// </summary>
  Task<IEnumerable<ProgramEntity>> GetProgramsByDifficultyAsync(
    ProgramDifficulty difficulty, int skip = 0,
    int take = 50
  );

  /// <summary>
  /// Get published programs for public access
  /// </summary>
  Task<IEnumerable<ProgramEntity>> GetPublishedProgramsAsync(int skip = 0, int take = 50);

  // Content Management with DTOs
  /// <summary>
  /// Add content to a program using DTO
  /// </summary>
  Task<ProgramContent?> AddContentAsync(Guid programId, CreateContentDto contentDto);

  /// <summary>
  /// Update program content using DTO
  /// </summary>
  Task<ProgramContent?> UpdateContentAsync(Guid programId, Guid contentId, UpdateContentDto contentDto);

  // User Progress with DTOs
  /// <summary>
  /// Update user progress using DTO
  /// </summary>
  Task<UserProgressDto?> UpdateUserProgressAsync(Guid programId, Guid userId, UpdateProgressDto progressDto);

  // Lifecycle Management
  /// <summary>
  /// Submit program for review
  /// </summary>
  Task<ProgramEntity?> SubmitProgramAsync(Guid id);

  /// <summary>
  /// Approve a program
  /// </summary>
  Task<ProgramEntity?> ApproveProgramAsync(Guid id);

  /// <summary>
  /// Reject a program with reason
  /// </summary>
  Task<ProgramEntity?> RejectProgramAsync(Guid id, string reason);

  /// <summary>
  /// Withdraw program from review
  /// </summary>
  Task<ProgramEntity?> WithdrawProgramAsync(Guid id);

  /// <summary>
  /// Archive a program
  /// </summary>
  Task<ProgramEntity?> ArchiveProgramAsync(Guid id);

  /// <summary>
  /// Restore an archived program
  /// </summary>
  Task<ProgramEntity?> RestoreProgramAsync(Guid id);

  // Publishing with Scheduling
  /// <summary>
  /// Schedule a program for publishing
  /// </summary>
  Task<ProgramEntity?> ScheduleProgramAsync(Guid id, DateTime publishAt);

  // Monetization Operations
  /// <summary>
  /// Enable monetization for a program
  /// </summary>
  Task<ProgramEntity?> EnableMonetizationAsync(Guid id, MonetizationDto monetizationDto);

  /// <summary>
  /// Disable monetization for a program
  /// </summary>
  Task<ProgramEntity?> DisableMonetizationAsync(Guid id);

  /// <summary>
  /// Get program pricing information
  /// </summary>
  Task<PricingDto?> GetProgramPricingAsync(Guid id);

  /// <summary>
  /// Update program pricing
  /// </summary>
  Task<PricingDto?> UpdateProgramPricingAsync(Guid id, UpdatePricingDto pricingDto);

  // Advanced Analytics
  /// <summary>
  /// Get comprehensive program analytics
  /// </summary>
  Task<ProgramAnalyticsDto?> GetProgramAnalyticsAsync(Guid id);

  /// <summary>
  /// Get user completion rates for a program
  /// </summary>
  Task<CompletionRatesDto?> GetCompletionRatesAsync(Guid id);

  /// <summary>
  /// Get program engagement metrics
  /// </summary>
  Task<EngagementMetricsDto?> GetEngagementMetricsAsync(Guid id);

  /// <summary>
  /// Get program revenue analytics
  /// </summary>
  Task<RevenueAnalyticsDto?> GetRevenueAnalyticsAsync(Guid id);

  // Product Integration
  /// <summary>
  /// Create a product from a program
  /// </summary>
  Task<Guid?> CreateProductFromProgramAsync(Guid programId, CreateProductFromProgramDto productDto);

  /// <summary>
  /// Link a program to an existing product
  /// </summary>
  Task<bool> LinkProgramToProductAsync(Guid programId, Guid productId);

  /// <summary>
  /// Unlink a program from a product
  /// </summary>
  Task<bool> UnlinkProgramFromProductAsync(Guid programId, Guid productId);

  /// <summary>
  /// Get all products linked to a program
  /// </summary>
  Task<IEnumerable<Guid>> GetLinkedProductsAsync(Guid programId);

  // Additional missing methods from controller

  /// <summary>
  /// Remove content from a program
  /// </summary>
  Task<bool> RemoveContentAsync(Guid programId, Guid contentId);

  /// <summary>
  /// Add a user to a program
  /// </summary>
  Task<UserProgressDto?> AddUserToProgramAsync(Guid programId, Guid userId);

  /// <summary>
  /// Remove a user from a program
  /// </summary>
  Task<bool> RemoveUserFromProgramAsync(Guid programId, Guid userId);

  /// <summary>
  /// Get all users in a program with pagination
  /// </summary>
  Task<IEnumerable<UserProgressDto>> GetProgramUsersAsync(Guid programId, int skip = 0, int take = 50);

  /// <summary>
  /// Mark content as completed for a user
  /// </summary>
  Task<bool> MarkContentCompletedAsync(Guid programId, Guid userId, Guid contentId);

  /// <summary>
  /// Reset user progress in a program
  /// </summary>
  Task<bool> ResetUserProgressAsync(Guid programId, Guid userId);
}
