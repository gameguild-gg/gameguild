using GameGuild.Modules.Contents;
using ProgramEntity = GameGuild.Modules.Programs.Program;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Service interface for comprehensive program business logic and operations
/// </summary>
/// <remarks>
/// IProgramService provides the complete contract for program management including:
/// - Basic CRUD operations for program lifecycle management
/// - Content management for hierarchical educational materials
/// - User participation tracking and enrollment management
/// - Progress analytics and reporting capabilities
/// - Advanced operations like cloning and bulk updates
/// 
/// This interface abstracts the complex business logic of educational program
/// management while providing clean, testable methods for controllers and handlers.
/// </remarks>
public interface IProgramService {
  // Basic CRUD Operations

  /// <summary>
  /// Retrieves a program by its unique identifier with basic details
  /// </summary>
  /// <param name="id">The unique identifier of the program</param>
  /// <returns>Program entity if found and not deleted, null otherwise</returns>
  /// <remarks>
  /// Returns only basic program information without related entities.
  /// Excludes soft-deleted programs from results.
  /// Use for lightweight program lookups and basic operations.
  /// </remarks>
  Task<ProgramEntity?> GetProgramByIdAsync(Guid id);

  /// <summary>
  /// Retrieves a program by its URL-friendly slug identifier
  /// </summary>
  /// <param name="slug">The URL-friendly slug of the program</param>
  /// <returns>Program entity if found and not deleted, null otherwise</returns>
  /// <remarks>
  /// Enables SEO-friendly URL routing and bookmarkable program links.
  /// Excludes soft-deleted programs from results.
  /// Used for public program access and content management.
  /// </remarks>
  Task<ProgramEntity?> GetProgramBySlugAsync(string slug);

  /// <summary>
  /// Retrieves a published program by slug that is publicly accessible
  /// </summary>
  /// <param name="slug">The URL-friendly slug of the program</param>
  /// <returns>Published program entity if found and accessible, null otherwise</returns>
  /// <remarks>
  /// Filters for published status and public visibility only.
  /// Used for public-facing program discovery and enrollment.
  /// Ensures only properly published content is accessible to learners.
  /// </remarks>
  Task<ProgramEntity?> GetPublishedProgramBySlugAsync(string slug);

  /// <summary>
  /// Retrieves a program with all associated content and user relationships
  /// </summary>
  /// <param name="id">The unique identifier of the program</param>
  /// <returns>Program entity with related content and users, null if not found</returns>
  /// <remarks>
  /// Includes ProgramContent and ProgramUser collections for complete program view.
  /// Excludes soft-deleted related entities from collections.
  /// Use for comprehensive program management and content editing.
  /// </remarks>
  Task<ProgramEntity?> GetProgramWithContentAsync(Guid id);

  /// <summary>
  /// Retrieves a paginated list of programs ordered by creation date
  /// </summary>
  /// <param name="skip">Number of programs to skip for pagination (default: 0)</param>
  /// <param name="take">Maximum number of programs to return (default: 50)</param>
  /// <returns>Collection of program entities within the specified range</returns>
  /// <remarks>
  /// Returns programs in descending order by creation date.
  /// Excludes soft-deleted programs from results.
  /// Used for program listing and administrative overview.
  /// </remarks>
  Task<IEnumerable<ProgramEntity>> GetProgramsAsync(int skip = 0, int take = 50);

  /// <summary>
  /// Creates a new program with initial draft status and private visibility
  /// </summary>
  /// <param name="program">Program entity to create with required properties populated</param>
  /// <returns>Created program entity with generated ID and timestamps</returns>
  /// <remarks>
  /// Automatically sets status to Draft and visibility to Private for safety.
  /// Generates UUID, creation timestamp, and initial metadata.
  /// Program must be explicitly published after content creation.
  /// </remarks>
  Task<ProgramEntity> CreateProgramAsync(ProgramEntity program);

  /// <summary>
  /// Updates an existing program's properties and metadata
  /// </summary>
  /// <param name="program">Program entity with updated properties and valid ID</param>
  /// <returns>Updated program entity with refreshed timestamps</returns>
  /// <remarks>
  /// Updates modification timestamp and version for change tracking.
  /// Preserves existing relationships and content associations.
  /// Use for program metadata, settings, and content organization changes.
  /// </remarks>
  Task<ProgramEntity> UpdateProgramAsync(ProgramEntity program);

  /// <summary>
  /// Soft deletes a program while preserving data integrity
  /// </summary>
  /// <param name="id">The unique identifier of the program to delete</param>
  /// <returns>Task representing the asynchronous operation</returns>
  /// <remarks>
  /// Sets DeletedAt timestamp without physically removing data.
  /// Preserves user enrollments and progress for historical records.
  /// Deleted programs are excluded from normal query operations.
  /// </remarks>
  Task DeleteProgramAsync(Guid id);

  /// <summary>
  /// Creates a complete copy of an existing program with new identity
  /// </summary>
  /// <param name="id">The unique identifier of the program to clone</param>
  /// <param name="newTitle">Title for the cloned program</param>
  /// <returns>New program entity with duplicated content and structure</returns>
  /// <remarks>
  /// Duplicates program metadata, content hierarchy, and settings.
  /// Creates new UUIDs for cloned program and all content items.
  /// Does not copy user enrollments or progress data.
  /// Useful for creating program templates and variations.
  /// </remarks>
  Task<ProgramEntity> CloneProgramAsync(Guid id, string newTitle);

  /// <summary>
  /// Verifies program existence and accessibility status
  /// </summary>
  /// <param name="id">The unique identifier of the program to check</param>
  /// <returns>True if program exists and is not deleted, false otherwise</returns>
  /// <remarks>
  /// Quick existence check without loading full program data.
  /// Used for validation and authorization operations.
  /// Excludes soft-deleted programs from existence confirmation.
  /// </remarks>
  Task<bool> ProgramExistsAsync(Guid id);

  // Content Management Operations

  /// <summary>
  /// Adds new educational content to a program's curriculum
  /// </summary>
  /// <param name="programId">The unique identifier of the target program</param>
  /// <param name="content">Content entity with type, title, and body populated</param>
  /// <returns>Created content entity with generated ID and program association</returns>
  /// <remarks>
  /// Automatically assigns sort order based on existing content count.
  /// Validates content type and required properties before creation.
  /// Supports hierarchical content through parent-child relationships.
  /// Updates program's modification timestamp for change tracking.
  /// </remarks>
  Task<ProgramContent> AddContentAsync(Guid programId, ProgramContent content);

  /// <summary>
  /// Updates existing program content properties and metadata
  /// </summary>
  /// <param name="content">Content entity with updated properties and valid ID</param>
  /// <returns>Updated content entity with refreshed timestamps</returns>
  /// <remarks>
  /// Preserves content hierarchy and learner progress associations.
  /// Updates modification timestamp for content versioning.
  /// Validates content type-specific requirements and constraints.
  /// </remarks>
  Task<ProgramContent> UpdateContentAsync(ProgramContent content);

  /// <summary>
  /// Removes content from a program while preserving learner interactions
  /// </summary>
  /// <param name="contentId">The unique identifier of the content to delete</param>
  /// <returns>Task representing the asynchronous operation</returns>
  /// <remarks>
  /// Soft deletes content to preserve interaction history and analytics.
  /// Automatically handles child content in hierarchical structures.
  /// Updates parent program's modification timestamp.
  /// </remarks>
  Task DeleteContentAsync(Guid contentId);

  /// <summary>
  /// Reorders program content according to specified sequence
  /// </summary>
  /// <param name="programId">The unique identifier of the target program</param>
  /// <param name="contentIds">Ordered list of content IDs in desired sequence</param>
  /// <returns>Updated program entity with reordered content</returns>
  /// <remarks>
  /// Updates sort order properties to match specified sequence.
  /// Affects learner navigation and content progression logic.
  /// Validates all content IDs belong to the specified program.
  /// </remarks>
  Task<ProgramEntity> ReorderContentAsync(Guid programId, List<Guid> contentIds);

  /// <summary>
  /// Retrieves all content items for a program in display order
  /// </summary>
  /// <param name="programId">The unique identifier of the target program</param>
  /// <returns>Ordered collection of content entities within the program</returns>
  /// <remarks>
  /// Returns content ordered by sort order for consistent display.
  /// Includes hierarchical parent-child relationships.
  /// Excludes soft-deleted content from results.
  /// </remarks>
  Task<IEnumerable<ProgramContent>> GetProgramContentAsync(Guid programId);

  // User Participation Management

  /// <summary>
  /// Enrolls a user in a program creating a participation relationship
  /// </summary>
  /// <param name="programId">The unique identifier of the target program</param>
  /// <param name="userId">The unique identifier of the user to enroll</param>
  /// <returns>Created ProgramUser entity representing the enrollment</returns>
  /// <remarks>
  /// Creates enrollment with initial progress tracking and timestamps.
  /// Validates program enrollment status and capacity constraints.
  /// Automatically activates the enrollment for immediate access.
  /// Triggers enrollment notifications and analytics updates.
  /// </remarks>
  Task<ProgramUser> AddUserAsync(Guid programId, Guid userId);

  /// <summary>
  /// Removes a user from a program while preserving progress history
  /// </summary>
  /// <param name="programId">The unique identifier of the target program</param>
  /// <param name="userId">The unique identifier of the user to remove</param>
  /// <returns>Updated ProgramUser entity with deactivated status</returns>
  /// <remarks>
  /// Deactivates enrollment without destroying progress data.
  /// Preserves interaction history for analytics and reporting.
  /// User loses access to program content but retains certificates.
  /// </remarks>
  Task<ProgramUser> RemoveUserAsync(Guid programId, Guid userId);

  /// <summary>
  /// Retrieves all users currently enrolled in a program
  /// </summary>
  /// <param name="programId">The unique identifier of the target program</param>
  /// <returns>Collection of ProgramUser entities with enrollment details</returns>
  /// <remarks>
  /// Includes enrollment status, progress percentages, and timestamps.
  /// Excludes inactive enrollments unless specifically requested.
  /// Used for program management and learner analytics.
  /// </remarks>
  Task<IEnumerable<ProgramUser>> GetProgramUsersAsync(Guid programId);

  /// <summary>
  /// Retrieves all programs where a user has active enrollment
  /// </summary>
  /// <param name="userId">The unique identifier of the target user</param>
  /// <returns>Collection of Program entities where user is enrolled</returns>
  /// <remarks>
  /// Returns programs with active enrollment status only.
  /// Includes program metadata and enrollment timestamps.
  /// Used for user dashboard and learning journey tracking.
  /// </remarks>
  Task<IEnumerable<ProgramEntity>> GetUserProgramsAsync(Guid userId);

  /// <summary>
  /// Verifies if a user has active enrollment in a specific program
  /// </summary>
  /// <param name="programId">The unique identifier of the target program</param>
  /// <param name="userId">The unique identifier of the user to check</param>
  /// <returns>True if user has active enrollment, false otherwise</returns>
  /// <remarks>
  /// Quick enrollment status check for access control.
  /// Considers only active enrollments, excludes deactivated ones.
  /// Used for authorization and content access validation.
  /// </remarks>
  Task<bool> IsUserInProgramAsync(Guid programId, Guid userId);

  // Progress & Analytics

  /// <summary>
  /// Retrieves comprehensive progress information for a user in a program
  /// </summary>
  /// <param name="programId">The unique identifier of the target program</param>
  /// <param name="userId">The unique identifier of the target user</param>
  /// <returns>Progress DTO with completion statistics and timeline data</returns>
  /// <remarks>
  /// Includes completion percentage, content interactions, and time tracking.
  /// Aggregates data from all content interactions within the program.
  /// Returns null if user is not enrolled or has no progress data.
  /// Used for progress visualization and learner dashboard.
  /// </remarks>
  Task<UserProgressDto?> GetUserProgressDtoAsync(Guid programId, Guid userId);

  /// <summary>
  /// Retrieves detailed interaction history for a user within a program
  /// </summary>
  /// <param name="programId">The unique identifier of the target program</param>
  /// <param name="userId">The unique identifier of the target user</param>
  /// <returns>Collection of content interactions with timestamps and progress</returns>
  /// <remarks>
  /// Includes views, submissions, completions, and time spent data.
  /// Ordered chronologically for timeline analysis.
  /// Used for detailed analytics and learning behavior insights.
  /// </remarks>
  Task<IEnumerable<ContentInteraction>> GetUserInteractionsAsync(Guid programId, Guid userId);

  /// <summary>
  /// Updates user's progress status for specific content within a program
  /// </summary>
  /// <param name="programId">The unique identifier of the target program</param>
  /// <param name="userId">The unique identifier of the target user</param>
  /// <param name="contentId">The unique identifier of the content item</param>
  /// <param name="status">New progress status to record</param>
  /// <returns>Updated program entity with refreshed progress calculations</returns>
  /// <remarks>
  /// Creates or updates content interaction records.
  /// Automatically recalculates overall program completion percentage.
  /// Triggers progress notifications and achievement checks.
  /// </remarks>
  Task<ProgramEntity> UpdateUserProgressAsync(Guid programId, Guid userId, Guid contentId, ProgressStatus status);

  // Lifecycle Management

  /// <summary>
  /// Creates a new program in draft status for content development
  /// </summary>
  /// <param name="program">Program entity with basic information populated</param>
  /// <returns>Created program entity in draft status with private visibility</returns>
  /// <remarks>
  /// Initializes program with safe defaults for content development.
  /// Draft programs are not visible to learners until published.
  /// Enables content creation and testing before public release.
  /// </remarks>
  Task<ProgramEntity> CreateDraftAsync(ProgramEntity program);

  /// <summary>
  /// Submits a draft program for review and approval workflow
  /// </summary>
  /// <param name="id">The unique identifier of the draft program</param>
  /// <returns>Updated program entity with review status</returns>
  /// <remarks>
  /// Transitions program from draft to pending review status.
  /// Validates program has required content and metadata.
  /// Triggers review workflow notifications to administrators.
  /// </remarks>
  Task<ProgramEntity> SubmitForReviewAsync(Guid id);

  /// <summary>
  /// Approves a reviewed program for publication and learner access
  /// </summary>
  /// <param name="id">The unique identifier of the program to approve</param>
  /// <returns>Updated program entity with published status</returns>
  /// <remarks>
  /// Transitions program to published status with public visibility.
  /// Enables learner enrollment and content access.
  /// Triggers publication notifications and discovery indexing.
  /// </remarks>
  Task<ProgramEntity> ApproveAsync(Guid id);

  /// <summary>
  /// Rejects a reviewed program with feedback for revision
  /// </summary>
  /// <param name="id">The unique identifier of the program to reject</param>
  /// <param name="reason">Detailed reason for rejection and required changes</param>
  /// <returns>Updated program entity with rejected status</returns>
  /// <remarks>
  /// Returns program to draft status with reviewer feedback.
  /// Preserves content and allows creator to address issues.
  /// Triggers rejection notification with improvement guidance.
  /// </remarks>
  Task<ProgramEntity> RejectAsync(Guid id, string reason);

  /// <summary>
  /// Archives a program while preserving learner access and progress
  /// </summary>
  /// <param name="id">The unique identifier of the program to archive</param>
  /// <returns>Updated program entity with archived status</returns>
  /// <remarks>
  /// Removes program from discovery while maintaining learner access.
  /// Enrolled learners can continue and complete the program.
  /// New enrollments are prevented but existing ones remain active.
  /// </remarks>
  Task<ProgramEntity> ArchiveAsync(Guid id);

  /// <summary>
  /// Publishes an approved program for public learner access
  /// </summary>
  /// <param name="id">The unique identifier of the program to publish</param>
  /// <returns>Updated program entity with published status</returns>
  /// <remarks>
  /// Makes program visible in public discovery and enrollment.
  /// Validates program meets publication requirements.
  /// Triggers indexing for search and recommendation systems.
  /// </remarks>
  Task<ProgramEntity> PublishAsync(Guid id);

  /// <summary>
  /// Sets program visibility level controlling access permissions
  /// </summary>
  /// <param name="id">The unique identifier of the target program</param>
  /// <param name="visibility">New visibility level (Public, Private, Unlisted)</param>
  /// <returns>Updated program entity with new visibility setting</returns>
  /// <remarks>
  /// Controls program discoverability and access permissions.
  /// Public: Visible in search and enrollment open to all
  /// Private: Invitation-only access with restricted visibility
  /// Unlisted: Accessible by direct link but not in discovery
  /// </remarks>
  Task<ProgramEntity> SetVisibilityAsync(Guid id, AccessLevel visibility);

  // Publishing Operations
  /// <summary> Publish a program </summary>
  Task<ProgramEntity> PublishProgramAsync(Guid id);

  /// <summary> Unpublish a program </summary>
  Task<ProgramEntity> UnpublishProgramAsync(Guid id);

  /// <summary> Schedule program for publishing </summary>
  Task<ProgramEntity> SchedulePublishAsync(Guid id, DateTime publishAt);

  // Search & Discovery

  /// <summary>
  /// Searches programs using text matching across titles, descriptions, and content
  /// </summary>
  /// <param name="searchTerm">Text to search for in program metadata and content</param>
  /// <param name="skip">Number of results to skip for pagination (default: 0)</param>
  /// <param name="take">Maximum number of results to return (default: 50)</param>
  /// <returns>Collection of programs matching the search criteria</returns>
  /// <remarks>
  /// Performs full-text search across program metadata and content.
  /// Results are ranked by relevance and filtered for published status.
  /// Includes fuzzy matching for improved search experience.
  /// </remarks>
  Task<IEnumerable<ProgramEntity>> SearchProgramsAsync(string searchTerm, int skip = 0, int take = 50);

  /// <summary>
  /// Retrieves programs created by a specific user or instructor
  /// </summary>
  /// <param name="creatorId">The unique identifier of the program creator</param>
  /// <param name="skip">Number of results to skip for pagination (default: 0)</param>
  /// <param name="take">Maximum number of results to return (default: 50)</param>
  /// <returns>Collection of programs created by the specified user</returns>
  /// <remarks>
  /// Includes programs in all states (draft, published, archived).
  /// Ordered by creation date with most recent first.
  /// Used for creator portfolio and content management.
  /// </remarks>
  Task<IEnumerable<ProgramEntity>> GetProgramsByCreatorAsync(Guid creatorId, int skip = 0, int take = 50);

  /// <summary>
  /// Retrieves curated featured programs for homepage and discovery
  /// </summary>
  /// <param name="count">Maximum number of featured programs to return (default: 10)</param>
  /// <returns>Collection of featured programs for promotion</returns>
  /// <remarks>
  /// Returns editorially selected programs for featured placement.
  /// Includes high-quality, popular, or strategically important content.
  /// Used for homepage highlights and promotional campaigns.
  /// </remarks>
  Task<IEnumerable<ProgramEntity>> GetFeaturedProgramsAsync(int count = 10);

  /// <summary>
  /// Retrieves recently published programs for discovery and trending
  /// </summary>
  /// <param name="count">Maximum number of recent programs to return (default: 10)</param>
  /// <returns>Collection of recently published programs</returns>
  /// <remarks>
  /// Returns newest published programs ordered by publication date.
  /// Filtered for published status and public visibility.
  /// Used for "what's new" sections and content discovery.
  /// </remarks>
  Task<IEnumerable<ProgramEntity>> GetRecentProgramsAsync(int count = 10);

  /// <summary>
  /// Retrieves popular programs based on enrollment and engagement metrics
  /// </summary>
  /// <param name="count">Maximum number of popular programs to return (default: 10)</param>
  /// <returns>Collection of popular programs ranked by engagement</returns>
  /// <remarks>
  /// Ranks programs by enrollment count, completion rate, and ratings.
  /// Weighted algorithm considers recency and sustained popularity.
  /// Used for trending sections and recommendation algorithms.
  /// </remarks>
  Task<IEnumerable<ProgramEntity>> GetPopularProgramsAsync(int count = 10);

  // Analytics & Statistics
  /// <summary> Get total program count with optional filters </summary>
  Task<int> GetProgramCountAsync(ContentStatus? status = null, AccessLevel? visibility = null);

  /// <summary> Get user count for a specific program </summary>
  Task<int> GetUserCountForProgramAsync(Guid programId);

  /// <summary> Get average completion rate for a program </summary>
  Task<decimal> GetAverageCompletionRateAsync(Guid programId);

  /// <summary> Get program completion statistics </summary>
  Task<Dictionary<string, object>> GetProgramStatisticsAsync(Guid programId);

  // Additional methods for controller support

  // CRUD Operations with DTOs
  /// <summary> Create a program using DTO </summary>
  Task<ProgramEntity> CreateProgramAsync(CreateProgramDto createDto);

  /// <summary> Update a program using DTO </summary>
  Task<ProgramEntity?> UpdateProgramAsync(Guid id, UpdateProgramDto updateDto);

  // Category and Difficulty Operations
  /// <summary> Get programs by category </summary>
  Task<IEnumerable<ProgramEntity>> GetProgramsByCategoryAsync(ProgramCategory category, int skip = 0, int take = 50);

  /// <summary> Get programs by difficulty level </summary>
  Task<IEnumerable<ProgramEntity>> GetProgramsByDifficultyAsync(ProgramDifficulty difficulty, int skip = 0, int take = 50);

  /// <summary> Get published programs for public access </summary>
  Task<IEnumerable<ProgramEntity>> GetPublishedProgramsAsync(int skip = 0, int take = 50);

  // Content Management with DTOs
  /// <summary> Add content to a program using DTO </summary>
  Task<ProgramContent?> AddContentAsync(Guid programId, CreateContentDto contentDto);

  /// <summary> Update program content using DTO </summary>
  Task<ProgramContent?> UpdateContentAsync(Guid programId, Guid contentId, UpdateContentDto contentDto);

  // User Progress with DTOs
  /// <summary> Update user progress using DTO </summary>
  Task<UserProgressDto?> UpdateUserProgressAsync(Guid programId, Guid userId, UpdateProgressDto progressDto);

  // Lifecycle Management
  /// <summary> Submit program for review </summary>
  Task<ProgramEntity?> SubmitProgramAsync(Guid id);

  /// <summary> Approve a program </summary>
  Task<ProgramEntity?> ApproveProgramAsync(Guid id);

  /// <summary> Reject a program with reason </summary>
  Task<ProgramEntity?> RejectProgramAsync(Guid id, string reason);

  /// <summary> Withdraw program from review </summary>
  Task<ProgramEntity?> WithdrawProgramAsync(Guid id);

  /// <summary> Archive a program </summary>
  Task<ProgramEntity?> ArchiveProgramAsync(Guid id);

  /// <summary> Restore an archived program </summary>
  Task<ProgramEntity?> RestoreProgramAsync(Guid id);

  // Publishing with Scheduling
  /// <summary> Schedule a program for publishing </summary>
  Task<ProgramEntity?> ScheduleProgramAsync(Guid id, DateTime publishAt);

  // Monetization Operations
  /// <summary> Enable monetization for a program </summary>
  Task<ProgramEntity?> EnableMonetizationAsync(Guid id, MonetizationDto monetizationDto);

  /// <summary> Disable monetization for a program </summary>
  Task<ProgramEntity?> DisableMonetizationAsync(Guid id);

  /// <summary> Get program pricing information </summary>
  Task<PricingDto?> GetProgramPricingAsync(Guid id);

  /// <summary> Update program pricing </summary>
  Task<PricingDto?> UpdateProgramPricingAsync(Guid id, UpdatePricingDto pricingDto);

  // Advanced Analytics
  /// <summary> Get comprehensive program analytics </summary>
  Task<ProgramAnalyticsDto?> GetProgramAnalyticsAsync(Guid id);

  /// <summary> Get user completion rates for a program </summary>
  Task<CompletionRatesDto?> GetCompletionRatesAsync(Guid id);

  /// <summary> Get program engagement metrics </summary>
  Task<EngagementMetricsDto?> GetEngagementMetricsAsync(Guid id);

  /// <summary> Get program revenue analytics </summary>
  Task<RevenueAnalyticsDto?> GetRevenueAnalyticsAsync(Guid id);

  // Product Integration
  /// <summary> Create a product from a program </summary>
  Task<Guid?> CreateProductFromProgramAsync(Guid programId, CreateProductFromProgramDto productDto);

  /// <summary> Link a program to an existing product </summary>
  Task<bool> LinkProgramToProductAsync(Guid programId, Guid productId);

  /// <summary> Unlink a program from a product </summary>
  Task<bool> UnlinkProgramFromProductAsync(Guid programId, Guid productId);

  /// <summary> Get all products linked to a program </summary>
  Task<IEnumerable<Guid>> GetLinkedProductsAsync(Guid programId);

  // Additional missing methods from controller

  /// <summary> Remove content from a program </summary>
  Task<bool> RemoveContentAsync(Guid programId, Guid contentId);

  /// <summary> Add a user to a program </summary>
  Task<UserProgressDto?> AddUserToProgramAsync(Guid programId, Guid userId);

  /// <summary> Remove a user from a program </summary>
  Task<bool> RemoveUserFromProgramAsync(Guid programId, Guid userId);

  /// <summary> Get all users in a program with pagination </summary>
  Task<IEnumerable<UserProgressDto>> GetProgramUsersAsync(Guid programId, int skip = 0, int take = 50);

  /// <summary> Mark content as completed for a user </summary>
  Task<bool> MarkContentCompletedAsync(Guid programId, Guid userId, Guid contentId);

  /// <summary> Reset user progress in a program </summary>
  Task<bool> ResetUserProgressAsync(Guid programId, Guid userId);
}
